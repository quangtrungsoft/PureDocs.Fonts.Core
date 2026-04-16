// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables.CmapTable;

/// <summary>
/// 'cmap' table — Character to glyph mapping.
/// Dispatches to subtable readers based on platformID/encodingID.
/// Priority: Format 12 (full Unicode) > Format 4 (BMP).
/// </summary>
internal sealed class CmapTable
{
    /// <summary>Segments for BMP lookups (from Format 4 or Format 12).</summary>
    public CmapSegment[] Segments { get; }

    /// <summary>True if this cmap covers supplementary planes (Format 12).</summary>
    public bool SupportsSupplementaryPlanes { get; }

    private CmapTable(CmapSegment[] segments, bool supportsSupplementary)
    {
        Segments = segments;
        SupportsSupplementaryPlanes = supportsSupplementary;
    }

    /// <summary>
    /// Parses the cmap table, selecting the best subtable.
    /// Priority: (3,10) Format 12 > (0,4) Format 12 > (3,1) Format 4 > (0,3) Format 4.
    /// </summary>
    public static CmapTable Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            throw new Parsing.InvalidFontException("cmap table too short.");

        var numTables = BinaryPrimitives.ReadUInt16BigEndian(data[2..]);

        // Scan for best subtable
        int bestOffset = -1;
        int bestPriority = -1;

        for (int i = 0; i < numTables; i++)
        {
            int recOff = 4 + i * 8;
            if (recOff + 8 > data.Length) break;

            var platformId = BinaryPrimitives.ReadUInt16BigEndian(data[recOff..]);
            var encodingId = BinaryPrimitives.ReadUInt16BigEndian(data[(recOff + 2)..]);
            var subtableOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(data[(recOff + 4)..]);

            int priority = GetPriority(platformId, encodingId);
            if (priority > bestPriority && subtableOffset < data.Length)
            {
                bestPriority = priority;
                bestOffset = subtableOffset;
            }
        }

        if (bestOffset < 0 || bestOffset + 2 > data.Length)
            throw new Parsing.InvalidFontException("No usable cmap subtable found.");

        var format = BinaryPrimitives.ReadUInt16BigEndian(data[bestOffset..]);

        return format switch
        {
            4 => new CmapTable(CmapFormat4Reader.Parse(data[bestOffset..]), supportsSupplementary: false),
            12 => new CmapTable(CmapFormat12Reader.Parse(data[bestOffset..]), supportsSupplementary: true),
            _ => throw new Parsing.InvalidFontException($"Unsupported cmap format {format}.")
        };
    }

    /// <summary>
    /// Lookup glyph ID for a Unicode codepoint via binary search on segments.
    /// Returns 0 (.notdef) if not found.
    /// </summary>
    public int GetGlyphId(int codepoint)
    {
        // Binary search on sorted segments
        int lo = 0, hi = Segments.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            ref readonly var seg = ref Segments[mid];

            if (codepoint < seg.StartCode)
                hi = mid - 1;
            else if (codepoint > seg.EndCode)
                lo = mid + 1;
            else
            {
                // Found the segment
                if (seg.GlyphIds != null)
                {
                    int idx = codepoint - seg.StartCode;
                    return idx < seg.GlyphIds.Length ? seg.GlyphIds[idx] : 0;
                }
                else
                {
                    // Delta-based mapping
                    return (codepoint + seg.IdDelta) & 0xFFFF;
                }
            }
        }

        return 0; // .notdef
    }

    private static int GetPriority(ushort platformId, ushort encodingId) =>
        (platformId, encodingId) switch
        {
            (3, 10) => 4, // Windows, Unicode full repertoire (Format 12)
            (0, 4) => 3,  // Unicode, Unicode full repertoire (Format 12)
            (3, 1) => 2,  // Windows, Unicode BMP (Format 4)
            (0, 3) => 1,  // Unicode, Unicode 2.0+ BMP
            _ => 0
        };
}

/// <summary>
/// Represents a contiguous range of codepoints mapped to glyph IDs.
/// </summary>
internal readonly struct CmapSegment
{
    public int StartCode { get; }
    public int EndCode { get; }

    /// <summary>
    /// Delta to add to codepoint to get glyph ID (when GlyphIds is null).
    /// Used for simple delta-based segments in Format 4.
    /// </summary>
    public int IdDelta { get; }

    /// <summary>
    /// Explicit glyph ID array for this segment. Null if delta-based.
    /// Index: codepoint - StartCode.
    /// </summary>
    public int[]? GlyphIds { get; }

    public CmapSegment(int startCode, int endCode, int idDelta, int[]? glyphIds = null)
    {
        StartCode = startCode;
        EndCode = endCode;
        IdDelta = idDelta;
        GlyphIds = glyphIds;
    }
}
