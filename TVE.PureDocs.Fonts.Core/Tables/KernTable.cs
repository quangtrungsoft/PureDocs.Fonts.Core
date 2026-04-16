// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'kern' table — Kerning table. Contains kerning pairs for horizontal layout.
/// Only format 0 (ordered pairs) is supported.
/// Optional table — may not be present in all fonts.
/// </summary>
internal sealed class KernTable
{
    /// <summary>
    /// Kerning pairs. Key: (leftGlyphId, rightGlyphId). Value: kerning value (font units).
    /// Positive = move apart, negative = move closer.
    /// </summary>
    public Dictionary<(ushort Left, ushort Right), short> Pairs { get; }

    private KernTable(Dictionary<(ushort Left, ushort Right), short> pairs)
    {
        Pairs = pairs;
    }

    /// <summary>
    /// Parses the kern table. Returns null if the table is empty or unsupported format.
    /// </summary>
    public static KernTable? Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return null;

        var version = BinaryPrimitives.ReadUInt16BigEndian(data);
        var nTables = BinaryPrimitives.ReadUInt16BigEndian(data[2..]);

        var pairs = new Dictionary<(ushort Left, ushort Right), short>();
        int offset = 4;

        for (int t = 0; t < nTables; t++)
        {
            if (offset + 6 > data.Length) break;

            // var subtableVersion = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            var subtableLength = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
            var coverage = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]);

            // Only process format 0, horizontal, non-cross-stream
            var format = (coverage >> 8) & 0xFF;
            var isHorizontal = (coverage & 0x0001) != 0;
            var isMinimum = (coverage & 0x0002) != 0;
            var isCrossStream = (coverage & 0x0004) != 0;

            if (format == 0 && isHorizontal && !isCrossStream && !isMinimum)
            {
                if (offset + 14 > data.Length) break;

                var nPairs = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..]);
                // searchRange, entrySelector, rangeShift at offset+8, +10, +12
                int pairOffset = offset + 14;

                for (int p = 0; p < nPairs; p++)
                {
                    if (pairOffset + 6 > data.Length) break;

                    var left = BinaryPrimitives.ReadUInt16BigEndian(data[pairOffset..]);
                    var right = BinaryPrimitives.ReadUInt16BigEndian(data[(pairOffset + 2)..]);
                    var value = BinaryPrimitives.ReadInt16BigEndian(data[(pairOffset + 4)..]);

                    pairs[(left, right)] = value;
                    pairOffset += 6;
                }
            }

            offset += subtableLength;
        }

        return pairs.Count > 0 ? new KernTable(pairs) : null;
    }
}
