// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'hmtx' table — Horizontal metrics. Contains advance widths and left side bearings.
/// Depends on hhea.numberOfHMetrics and maxp.numGlyphs.
/// </summary>
internal sealed class HmtxTable
{
    /// <summary>Advance widths indexed by glyph ID (font units).</summary>
    public ushort[] AdvanceWidths { get; }

    /// <summary>Left side bearings indexed by glyph ID (font units).</summary>
    public short[] LeftSideBearings { get; }

    private HmtxTable(ushort[] advanceWidths, short[] leftSideBearings)
    {
        AdvanceWidths = advanceWidths;
        LeftSideBearings = leftSideBearings;
    }

    /// <summary>
    /// Parses the hmtx table.
    /// </summary>
    /// <param name="data">Raw hmtx table data.</param>
    /// <param name="numberOfHMetrics">From hhea table.</param>
    /// <param name="numGlyphs">From maxp table.</param>
    public static HmtxTable Parse(ReadOnlySpan<byte> data, ushort numberOfHMetrics, ushort numGlyphs)
    {
        var advanceWidths = new ushort[numGlyphs];
        var lsbs = new short[numGlyphs];
        int offset = 0;

        // First: numberOfHMetrics longHorMetric records (4 bytes each)
        ushort lastAdvanceWidth = 0;
        for (int i = 0; i < numberOfHMetrics; i++)
        {
            if (offset + 4 > data.Length)
                throw new Parsing.InvalidFontException("hmtx table truncated in hMetrics.");

            lastAdvanceWidth = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            advanceWidths[i] = lastAdvanceWidth;
            lsbs[i] = BinaryPrimitives.ReadInt16BigEndian(data[(offset + 2)..]);
            offset += 4;
        }

        // Remaining glyphs: reuse last advance width, only LSB varies
        for (int i = numberOfHMetrics; i < numGlyphs; i++)
        {
            advanceWidths[i] = lastAdvanceWidth;
            if (offset + 2 <= data.Length)
            {
                lsbs[i] = BinaryPrimitives.ReadInt16BigEndian(data[offset..]);
                offset += 2;
            }
        }

        return new HmtxTable(advanceWidths, lsbs);
    }
}
