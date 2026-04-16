// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Mapping;
using TVE.PureDocs.Fonts.Core.Metrics;
using TVE.PureDocs.Fonts.Core.Tables;
using TVE.PureDocs.Fonts.Core.Tables.CmapTable;

namespace TVE.PureDocs.Fonts.Core.Parsing;

/// <summary>
/// Entry point: ReadOnlyMemory&lt;byte&gt; → TtfFontData.
/// Zero-copy: uses ReadOnlyMemory to slice tables in-place.
/// Thread-safe: returns immutable TtfFontData.
/// </summary>
public static class TtfFontReader
{
    /// <summary>
    /// Parse TTF/OTF font from raw bytes.
    /// </summary>
    /// <exception cref="InvalidFontException">Font binary invalid or missing required tables.</exception>
    public static TtfFontData Parse(ReadOnlyMemory<byte> fontData)
    {
        var span = fontData.Span;

        // Step 1: Offset Table
        var offsetTable = OffsetTable.Parse(span);
        if (!offsetTable.IsTrueType && !offsetTable.IsCff)
            throw new InvalidFontException(
                $"Unknown sfVersion: 0x{offsetTable.SfVersion:X8}. Expected TrueType or CFF.");

        // Step 1b: Parse all table records
        var records = TableRecord.ParseAll(span, offsetTable.NumTables);
        var tableMap = new Dictionary<string, ReadOnlyMemory<byte>>(records.Length);

        foreach (var rec in records)
        {
            if (rec.Offset + rec.Length <= (uint)fontData.Length)
                tableMap[rec.Tag] = fontData.Slice((int)rec.Offset, (int)rec.Length);
        }

        // Helper to get required table data
        ReadOnlySpan<byte> RequireTable(string tag)
        {
            if (!tableMap.TryGetValue(tag, out var data))
                throw new InvalidFontException($"Required table '{tag}' not found.");
            return data.Span;
        }

        // Step 2: head
        var head = HeadTable.Parse(RequireTable("head"));

        // Step 3: hhea
        var hhea = HheaTable.Parse(RequireTable("hhea"));

        // Step 4: maxp
        var maxp = MaxpTable.Parse(RequireTable("maxp"));

        // Step 5: cmap
        var cmap = Tables.CmapTable.CmapTable.Parse(RequireTable("cmap"));

        // Step 6: hmtx
        var hmtx = HmtxTable.Parse(RequireTable("hmtx"), hhea.NumberOfHMetrics, maxp.NumGlyphs);

        // Step 7: loca (required for TrueType, not for CFF)
        LocaTable? loca = null;
        if (!offsetTable.IsCff && tableMap.ContainsKey("loca"))
            loca = LocaTable.Parse(RequireTable("loca"), head.IndexToLocFormat, maxp.NumGlyphs);

        // Step 9: OS/2
        Os2Table? os2 = null;
        if (tableMap.ContainsKey("OS/2"))
            os2 = Os2Table.Parse(tableMap["OS/2"].Span);

        // Step 10: post
        PostTable? post = null;
        if (tableMap.ContainsKey("post"))
            post = PostTable.Parse(tableMap["post"].Span);

        // Step 11: name
        var name = NameTable.Parse(RequireTable("name"));

        // Step 12: kern (optional)
        KernTable? kern = null;
        if (tableMap.ContainsKey("kern"))
            kern = KernTable.Parse(tableMap["kern"].Span);

        // Build identity flags
        bool isBold = (os2?.IsBold ?? false) || head.IsBold;
        bool isItalic = (os2?.IsItalic ?? false) || head.IsItalic;
        bool isCff = offsetTable.IsCff;

        // Build metrics
        var metrics = FontMetricsExtractor.Extract(head, hhea, os2, post);

        // Build advance width table
        var advanceWidths = new AdvanceWidthTable(hmtx.AdvanceWidths, head.UnitsPerEm);

        // Build kerning pair table
        KerningPairTable? kerningPairs = null;
        if (kern != null)
            kerningPairs = new KerningPairTable(kern.Pairs, head.UnitsPerEm);

        // Build glyph mapper
        var glyphMapper = new GlyphMapper(cmap, loca, tableMap.ContainsKey("glyf") ? tableMap["glyf"] : ReadOnlyMemory<byte>.Empty);

        return new TtfFontData(
            familyName: name.FamilyName,
            subfamilyName: name.SubfamilyName,
            postScriptName: name.PostScriptName,
            isBold: isBold,
            isItalic: isItalic,
            isCff: isCff,
            metrics: metrics,
            advanceWidths: advanceWidths,
            kerningPairs: kerningPairs,
            glyphMapper: glyphMapper,
            rawTables: tableMap,
            glyphCount: maxp.NumGlyphs,
            unitsPerEm: head.UnitsPerEm
        );
    }

    /// <summary>Convenience: read from file path.</summary>
    public static TtfFontData ParseFile(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        return Parse(data);
    }
}
