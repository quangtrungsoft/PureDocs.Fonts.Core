// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables.CmapTable;

/// <summary>
/// CMap Format 4 reader — Segment mapping to delta values.
/// Covers BMP (U+0000..U+FFFF).
///
/// Structure:
///   format(2) length(2) language(2) segCountX2(2)
///   searchRange(2) entrySelector(2) rangeShift(2)
///   endCode[segCount]       uint16[]
///   reservedPad             uint16 = 0
///   startCode[segCount]     uint16[]
///   idDelta[segCount]       int16[]
///   idRangeOffset[segCount] uint16[]
///   glyphIdArray[]          uint16[]
/// </summary>
internal static class CmapFormat4Reader
{
    public static CmapSegment[] Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 14)
            throw new Parsing.InvalidFontException("cmap format 4 subtable too short.");

        // var format = BinaryPrimitives.ReadUInt16BigEndian(data);  // = 4
        var length = BinaryPrimitives.ReadUInt16BigEndian(data[2..]);
        var segCountX2 = BinaryPrimitives.ReadUInt16BigEndian(data[6..]);
        int segCount = segCountX2 / 2;

        if (segCount == 0)
            return Array.Empty<CmapSegment>();

        // Array offsets (relative to subtable start)
        int endCodeBase = 14;
        int startCodeBase = endCodeBase + segCountX2 + 2; // +2 for reservedPad
        int idDeltaBase = startCodeBase + segCountX2;
        int idRangeOffsetBase = idDeltaBase + segCountX2;
        int glyphIdArrayBase = idRangeOffsetBase + segCountX2;

        var segments = new List<CmapSegment>(segCount);

        for (int i = 0; i < segCount; i++)
        {
            int endOff = endCodeBase + i * 2;
            int startOff = startCodeBase + i * 2;
            int deltaOff = idDeltaBase + i * 2;
            int rangeOff = idRangeOffsetBase + i * 2;

            if (rangeOff + 2 > data.Length) break;

            ushort endCode = BinaryPrimitives.ReadUInt16BigEndian(data[endOff..]);
            ushort startCode = BinaryPrimitives.ReadUInt16BigEndian(data[startOff..]);
            short idDelta = BinaryPrimitives.ReadInt16BigEndian(data[deltaOff..]);
            ushort idRangeOffset = BinaryPrimitives.ReadUInt16BigEndian(data[rangeOff..]);

            // Skip sentinel segment (0xFFFF)
            if (startCode == 0xFFFF) continue;

            if (idRangeOffset == 0)
            {
                // Delta-based mapping: glyphId = (codepoint + idDelta) & 0xFFFF
                segments.Add(new CmapSegment(startCode, endCode, idDelta));
            }
            else
            {
                // idRangeOffset-based: index into glyphIdArray
                int count = endCode - startCode + 1;
                var glyphIds = new int[count];

                for (int c = 0; c < count; c++)
                {
                    // PITFALL: idRangeOffset is relative to the position of the field itself
                    // Formula: glyphIdArrayIdx = idRangeOffset/2 + (c - startCode + startCode) + (i - segCount)
                    // Simplified: byte offset from rangeOff
                    int glyphIdOffset = rangeOff + idRangeOffset + c * 2;

                    if (glyphIdOffset + 2 <= data.Length)
                    {
                        ushort rawGlyphId = BinaryPrimitives.ReadUInt16BigEndian(data[glyphIdOffset..]);
                        glyphIds[c] = rawGlyphId != 0 ? (rawGlyphId + idDelta) & 0xFFFF : 0;
                    }
                }

                segments.Add(new CmapSegment(startCode, endCode, 0, glyphIds));
            }
        }

        return segments.ToArray();
    }
}
