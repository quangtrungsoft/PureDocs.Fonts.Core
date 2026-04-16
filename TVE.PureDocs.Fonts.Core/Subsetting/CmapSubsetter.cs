// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Subsetting;

/// <summary>
/// Rebuilds a cmap format 4 subtable for the subset.
/// Maps only the characters that exist in the subset.
/// </summary>
internal static class CmapSubsetter
{
    /// <summary>
    /// Builds a minimal cmap table with a format 4 subtable for the subset.
    /// </summary>
    /// <param name="charToNewGlyphId">Unicode char → new glyph ID mapping.</param>
    public static byte[] Build(IReadOnlyDictionary<char, int> charToNewGlyphId)
    {
        // Sort characters
        var sorted = new List<KeyValuePair<char, int>>(charToNewGlyphId);
        sorted.Sort((a, b) => a.Key.CompareTo(b.Key));

        // Build segments
        var segments = new List<(ushort startCode, ushort endCode, List<ushort> glyphIds)>();

        if (sorted.Count > 0)
        {
            ushort segStart = sorted[0].Key;
            ushort segEnd = sorted[0].Key;
            var segGlyphs = new List<ushort> { (ushort)sorted[0].Value };

            for (int i = 1; i < sorted.Count; i++)
            {
                ushort code = sorted[i].Key;
                if (code == segEnd + 1)
                {
                    segEnd = code;
                    segGlyphs.Add((ushort)sorted[i].Value);
                }
                else
                {
                    segments.Add((segStart, segEnd, segGlyphs));
                    segStart = code;
                    segEnd = code;
                    segGlyphs = new List<ushort> { (ushort)sorted[i].Value };
                }
            }

            segments.Add((segStart, segEnd, segGlyphs));
        }

        // Add sentinel segment
        int segCount = segments.Count + 1; // +1 for sentinel

        // Calculate format 4 subtable size
        int glyphIdArraySize = 0;
        foreach (var seg in segments)
            glyphIdArraySize += seg.glyphIds.Count;

        int subtableSize = 14 + segCount * 8 + glyphIdArraySize * 2;

        // Build cmap header (4 bytes) + encoding record (8 bytes) + format 4 subtable
        int totalSize = 4 + 8 + subtableSize;
        var result = new byte[totalSize];
        var span = result.AsSpan();

        // cmap header
        BinaryPrimitives.WriteUInt16BigEndian(span, 0); // version
        BinaryPrimitives.WriteUInt16BigEndian(span[2..], 1); // numTables

        // Encoding record: platform=3 (Windows), encoding=1 (Unicode BMP)
        BinaryPrimitives.WriteUInt16BigEndian(span[4..], 3); // platformID
        BinaryPrimitives.WriteUInt16BigEndian(span[6..], 1); // encodingID
        BinaryPrimitives.WriteUInt32BigEndian(span[8..], 12); // subtable offset

        // Format 4 subtable
        int off = 12;
        BinaryPrimitives.WriteUInt16BigEndian(span[off..], 4); // format
        BinaryPrimitives.WriteUInt16BigEndian(span[(off + 2)..], (ushort)subtableSize); // length
        BinaryPrimitives.WriteUInt16BigEndian(span[(off + 4)..], 0); // language

        ushort segCountX2 = (ushort)(segCount * 2);
        BinaryPrimitives.WriteUInt16BigEndian(span[(off + 6)..], segCountX2);

        // searchRange, entrySelector, rangeShift
        int searchRange = 1;
        int entrySelector = 0;
        while (searchRange * 2 <= segCount)
        {
            searchRange *= 2;
            entrySelector++;
        }

        searchRange *= 2;
        int rangeShift = segCountX2 - searchRange;

        BinaryPrimitives.WriteUInt16BigEndian(span[(off + 8)..], (ushort)searchRange);
        BinaryPrimitives.WriteUInt16BigEndian(span[(off + 10)..], (ushort)entrySelector);
        BinaryPrimitives.WriteUInt16BigEndian(span[(off + 12)..], (ushort)rangeShift);

        // Arrays
        int endCodeOff = off + 14;
        int reservedPadOff = endCodeOff + segCount * 2;
        int startCodeOff = reservedPadOff + 2;
        int idDeltaOff = startCodeOff + segCount * 2;
        int idRangeOffsetOff = idDeltaOff + segCount * 2;
        int glyphIdArrayOff = idRangeOffsetOff + segCount * 2;

        int glyphArrayIndex = 0;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            BinaryPrimitives.WriteUInt16BigEndian(span[(endCodeOff + i * 2)..], seg.endCode);
            BinaryPrimitives.WriteUInt16BigEndian(span[(startCodeOff + i * 2)..], seg.startCode);

            // Use idRangeOffset-based mapping for all segments (simpler)
            int rangeOffset = (segCount - i) * 2 + glyphArrayIndex * 2;
            BinaryPrimitives.WriteUInt16BigEndian(span[(idRangeOffsetOff + i * 2)..], (ushort)rangeOffset);
            BinaryPrimitives.WriteInt16BigEndian(span[(idDeltaOff + i * 2)..], 0); // delta = 0

            // Write glyph IDs
            foreach (var gid in seg.glyphIds)
            {
                BinaryPrimitives.WriteUInt16BigEndian(span[(glyphIdArrayOff + glyphArrayIndex * 2)..], gid);
                glyphArrayIndex++;
            }
        }

        // Sentinel segment
        int sentinelIdx = segments.Count;
        BinaryPrimitives.WriteUInt16BigEndian(span[(endCodeOff + sentinelIdx * 2)..], 0xFFFF);
        BinaryPrimitives.WriteUInt16BigEndian(span[(startCodeOff + sentinelIdx * 2)..], 0xFFFF);
        BinaryPrimitives.WriteInt16BigEndian(span[(idDeltaOff + sentinelIdx * 2)..], 1);
        BinaryPrimitives.WriteUInt16BigEndian(span[(idRangeOffsetOff + sentinelIdx * 2)..], 0);

        // Reserved pad
        BinaryPrimitives.WriteUInt16BigEndian(span[reservedPadOff..], 0);

        return result;
    }
}
