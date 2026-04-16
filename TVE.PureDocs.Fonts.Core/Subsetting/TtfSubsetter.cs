// Licensed to TVE under the MIT License.

using System.Buffers.Binary;
using TVE.PureDocs.Fonts.Core.Mapping;
using TVE.PureDocs.Fonts.Core.Metrics;
using TVE.PureDocs.Fonts.Core.Parsing;

namespace TVE.PureDocs.Fonts.Core.Subsetting;

/// <summary>
/// Creates TTF/OTF font subsets. Input: TtfFontData + GlyphSet. Output: SubsetResult.
///
/// Algorithm (TrueType):
///   1. Validate: add .notdef (glyphId=0), composites already expanded in GlyphSet
///   2. Reindex: sort old IDs → assign new sequential IDs
///   3. Rebuild glyf: copy glyph bytes, patch composite component IDs
///   4. Rebuild loca: new offsets (auto-choose short vs long format)
///   5. Rebuild hmtx: subset advance widths + LSBs
///   6. Rebuild cmap format 4: only char→glyphId in subset
///   7. Update head (indexToLocFormat), hhea, maxp (numGlyphs)
///   8. Copy OS/2, post, name verbatim
///   9. Rebuild Offset Table (new table offsets, 4-byte aligned)
///   10. Recalculate all table checksums + head.checkSumAdjustment
/// </summary>
public sealed class TtfSubsetter
{
    private readonly TtfFontData _font;
    private static readonly Random _random = new();

    public TtfSubsetter(TtfFontData font) => _font = font;

    /// <summary>Creates a subset from a GlyphSet (composites already expanded).</summary>
    public SubsetResult CreateSubset(GlyphSet glyphs)
    {
        return BuildSubset(glyphs, charToGlyphMap: null, glyphToCodepoints: null);
    }

    /// <summary>Shorthand: build GlyphSet from chars then subset.</summary>
    public SubsetResult CreateSubset(IEnumerable<char> chars)
    {
        var charList = chars.Distinct().ToList();
        var glyphSet = _font.GlyphMapper.BuildGlyphSet(charList);

        // Build char → old glyph map
        var charToGlyph = new Dictionary<char, int>();
        foreach (var c in charList)
        {
            int gid = _font.GlyphMapper.GetGlyphId(c);
            if (gid > 0) charToGlyph[c] = gid;
        }

        return BuildSubset(glyphSet, charToGlyph, glyphToCodepoints: null);
    }

    /// <summary>
    /// Shorthand: from ShapedGlyphs (glyph IDs from DirectWrite).
    /// </summary>
    public SubsetResult CreateSubsetFromGlyphs(
        IEnumerable<int> shapedGlyphIds,
        IReadOnlyDictionary<int, ReadOnlyMemory<int>> glyphToCodepoints)
    {
        var glyphSet = _font.GlyphMapper.BuildGlyphSetFromShaped(shapedGlyphIds);
        return BuildSubset(glyphSet, charToGlyphMap: null, glyphToCodepoints);
    }

    private SubsetResult BuildSubset(
        GlyphSet glyphs,
        Dictionary<char, int>? charToGlyphMap,
        IReadOnlyDictionary<int, ReadOnlyMemory<int>>? glyphToCodepoints)
    {
        // Step 1: Sort glyph IDs and build remap
        var sortedOldGlyphs = glyphs.ToSortedList();
        var glyphIdRemap = new Dictionary<int, int>();
        for (int i = 0; i < sortedOldGlyphs.Count; i++)
            glyphIdRemap[sortedOldGlyphs[i]] = i;

        int numSubsetGlyphs = sortedOldGlyphs.Count;

        // Build char → new glyph ID map
        var charToNewGlyphId = new Dictionary<char, int>();
        if (charToGlyphMap != null)
        {
            foreach (var (c, oldGid) in charToGlyphMap)
            {
                if (glyphIdRemap.TryGetValue(oldGid, out var newGid))
                    charToNewGlyphId[c] = newGid;
            }
        }

        // Build new glyph → codepoints map
        var newGlyphToCodepoints = new Dictionary<int, ReadOnlyMemory<int>>();
        if (glyphToCodepoints != null)
        {
            foreach (var (oldGid, codepoints) in glyphToCodepoints)
            {
                if (glyphIdRemap.TryGetValue(oldGid, out var newGid))
                    newGlyphToCodepoints[newGid] = codepoints;
            }
        }

        // CFF fonts: different path
        if (_font.IsCff)
            return BuildCffSubset(sortedOldGlyphs, glyphIdRemap, charToNewGlyphId,
                newGlyphToCodepoints, numSubsetGlyphs);

        // TrueType subsetting
        return BuildTrueTypeSubset(sortedOldGlyphs, glyphIdRemap, charToNewGlyphId,
            newGlyphToCodepoints, numSubsetGlyphs);
    }

    private SubsetResult BuildTrueTypeSubset(
        List<int> sortedOldGlyphs,
        Dictionary<int, int> glyphIdRemap,
        Dictionary<char, int> charToNewGlyphId,
        Dictionary<int, ReadOnlyMemory<int>> newGlyphToCodepoints,
        int numSubsetGlyphs)
    {
        var glyfData = _font.GetRawTable("glyf").Span;
        var locaRaw = _font.GetRawTable("loca").Span;
        var hmtxRaw = _font.GetRawTable("hmtx").Span;
        var headRaw = _font.GetRawTable("head");
        var hheaRaw = _font.GetRawTable("hhea");
        var maxpRaw = _font.GetRawTable("maxp");

        // Parse loca offsets for subsetter
        var headParsed = Tables.HeadTable.Parse(headRaw.Span);
        var hheaParsed = Tables.HheaTable.Parse(hheaRaw.Span);
        var locaParsed = Tables.LocaTable.Parse(locaRaw, headParsed.IndexToLocFormat,
            (ushort)(_font.GlyphCount));

        // Step 3-4: Rebuild glyf + loca
        var (newGlyf, newLoca) = GlyfTableSubsetter.Build(
            glyfData, locaParsed.Offsets, sortedOldGlyphs, glyphIdRemap, out bool useShortLoca);

        // Step 5: Rebuild hmtx
        var (newHmtx, subsetWidths) = HmtxSubsetter.Build(
            hmtxRaw, hheaParsed.NumberOfHMetrics, sortedOldGlyphs);

        // Step 6: Rebuild cmap
        var newCmap = CmapSubsetter.Build(charToNewGlyphId);

        // Step 7: Update head, hhea, maxp
        var newHead = headRaw.ToArray();
        // Update indexToLocFormat
        BinaryPrimitives.WriteInt16BigEndian(newHead.AsSpan(50), (short)(useShortLoca ? 0 : 1));

        var newHhea = hheaRaw.ToArray();
        // Update numberOfHMetrics = numSubsetGlyphs (all entries are longHorMetric)
        BinaryPrimitives.WriteUInt16BigEndian(newHhea.AsSpan(34), (ushort)numSubsetGlyphs);

        var newMaxp = maxpRaw.ToArray();
        // Update numGlyphs
        BinaryPrimitives.WriteUInt16BigEndian(newMaxp.AsSpan(4), (ushort)numSubsetGlyphs);

        // Step 8: Copy optional tables verbatim
        var tables = new List<(string Tag, byte[] Data)>
        {
            ("head", newHead),
            ("hhea", newHhea),
            ("maxp", newMaxp),
            ("hmtx", newHmtx),
            ("glyf", newGlyf),
            ("loca", newLoca),
            ("cmap", newCmap),
        };

        // Minimal post table
        tables.Add(("post", BuildMinimalPost()));

        // Copy OS/2 and name verbatim if present
        CopyTableIfPresent(tables, "OS/2");
        CopyTableIfPresent(tables, "name");

        // Copy hinting tables if present
        foreach (var tag in new[] { "cvt ", "fpgm", "prep" })
            CopyTableIfPresent(tables, tag);

        // Step 9-10: Build final font file
        var fontFile = BuildFontFile(tables);

        // Calculate head.checkSumAdjustment
        int headOffset = FindTableOffset(fontFile, "head");
        if (headOffset >= 0)
            TableChecksumCalculator.CalculateHeadCheckSumAdjustment(fontFile, headOffset);

        // Build subset advance width table
        var subsetAdvanceWidths = new AdvanceWidthTable(subsetWidths, _font.UnitsPerEm);

        return new SubsetResult
        {
            SubsetBytes = fontFile,
            GlyphIdRemap = glyphIdRemap,
            CharToNewGlyphId = charToNewGlyphId,
            GlyphToCodepoints = newGlyphToCodepoints,
            SubsetGlyphCount = numSubsetGlyphs,
            SubsetAdvanceWidths = subsetAdvanceWidths,
            PrefixedFontName = GeneratePrefix() + "+" + _font.PostScriptName
        };
    }

    private SubsetResult BuildCffSubset(
        List<int> sortedOldGlyphs,
        Dictionary<int, int> glyphIdRemap,
        Dictionary<char, int> charToNewGlyphId,
        Dictionary<int, ReadOnlyMemory<int>> newGlyphToCodepoints,
        int numSubsetGlyphs)
    {
        var cffData = _font.GetRawTable("CFF ").Span;
        if (cffData.IsEmpty)
            cffData = _font.GetRawTable("CFF2").Span;

        var subsetCff = CffSubsetter.Subset(cffData, sortedOldGlyphs);

        // For CFF fonts, we build a minimal font with just the CFF data
        var tables = new List<(string Tag, byte[] Data)>();
        tables.Add(("CFF ", subsetCff));

        // Copy required tables
        foreach (var tag in new[] { "head", "hhea", "maxp", "OS/2", "name", "post" })
            CopyTableIfPresent(tables, tag);

        var fontFile = BuildFontFile(tables);

        return new SubsetResult
        {
            SubsetBytes = fontFile,
            GlyphIdRemap = glyphIdRemap,
            CharToNewGlyphId = charToNewGlyphId,
            GlyphToCodepoints = newGlyphToCodepoints,
            SubsetGlyphCount = numSubsetGlyphs,
            PrefixedFontName = GeneratePrefix() + "+" + _font.PostScriptName
        };
    }

    private void CopyTableIfPresent(List<(string Tag, byte[] Data)> tables, string tag)
    {
        var data = _font.GetRawTable(tag);
        if (!data.IsEmpty)
            tables.Add((tag, data.ToArray()));
    }

    private static byte[] BuildMinimalPost()
    {
        // Version 3.0 post table (no glyph names)
        var data = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(data, 0x00030000); // version 3.0
        // All other fields default to 0
        return data;
    }

    private static byte[] BuildFontFile(List<(string Tag, byte[] Data)> tables)
    {
        int numTables = tables.Count;

        // Offset table (12 bytes) + table records (16 bytes each)
        int headerSize = 12 + numTables * 16;
        int dataOffset = (headerSize + 3) & ~3; // 4-byte aligned

        // Calculate total size
        int totalSize = dataOffset;
        foreach (var (_, data) in tables)
            totalSize += (data.Length + 3) & ~3; // 4-byte aligned

        var result = new byte[totalSize];
        var span = result.AsSpan();

        // Offset table
        BinaryPrimitives.WriteUInt32BigEndian(span, 0x00010000); // sfVersion (TrueType)
        BinaryPrimitives.WriteUInt16BigEndian(span[4..], (ushort)numTables);

        int searchRange = 1;
        int entrySelector = 0;
        while (searchRange * 2 <= numTables)
        {
            searchRange *= 2;
            entrySelector++;
        }

        searchRange *= 16;
        int rangeShift = numTables * 16 - searchRange;
        BinaryPrimitives.WriteUInt16BigEndian(span[6..], (ushort)searchRange);
        BinaryPrimitives.WriteUInt16BigEndian(span[8..], (ushort)entrySelector);
        BinaryPrimitives.WriteUInt16BigEndian(span[10..], (ushort)rangeShift);

        // Table records and data
        int currentDataOffset = dataOffset;
        for (int i = 0; i < numTables; i++)
        {
            var (tag, data) = tables[i];
            int recOff = 12 + i * 16;

            // Write tag (4 ASCII bytes)
            for (int j = 0; j < 4 && j < tag.Length; j++)
                span[recOff + j] = (byte)tag[j];
            for (int j = tag.Length; j < 4; j++)
                span[recOff + j] = (byte)' ';

            // Checksum
            uint checksum = TableChecksumCalculator.Calculate(data);
            BinaryPrimitives.WriteUInt32BigEndian(span[(recOff + 4)..], checksum);

            // Offset
            BinaryPrimitives.WriteUInt32BigEndian(span[(recOff + 8)..], (uint)currentDataOffset);

            // Length
            BinaryPrimitives.WriteUInt32BigEndian(span[(recOff + 12)..], (uint)data.Length);

            // Copy data
            data.CopyTo(span[currentDataOffset..]);
            currentDataOffset += (data.Length + 3) & ~3; // 4-byte aligned
        }

        return result;
    }

    private static int FindTableOffset(byte[] fontFile, string tag)
    {
        var span = fontFile.AsSpan();
        int numTables = BinaryPrimitives.ReadUInt16BigEndian(span[4..]);

        for (int i = 0; i < numTables; i++)
        {
            int recOff = 12 + i * 16;
            if (recOff + 16 > span.Length) break;

            bool match = true;
            for (int j = 0; j < tag.Length && j < 4; j++)
            {
                if (span[recOff + j] != (byte)tag[j]) { match = false; break; }
            }

            if (match)
                return (int)BinaryPrimitives.ReadUInt32BigEndian(span[(recOff + 8)..]);
        }

        return -1;
    }

    private static string GeneratePrefix()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Span<char> prefix = stackalloc char[6];
        for (int i = 0; i < 6; i++)
            prefix[i] = chars[_random.Next(chars.Length)];
        return new string(prefix);
    }
}
