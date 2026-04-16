// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Subsetting;

/// <summary>
/// Subsets the hmtx table to include only advance widths and LSBs for the subset glyphs.
/// </summary>
internal static class HmtxSubsetter
{
    /// <summary>
    /// Builds a subset hmtx table.
    /// Each entry is a longHorMetric record (4 bytes: advanceWidth + lsb).
    /// </summary>
    public static (byte[] Data, ushort[] AdvanceWidths) Build(
        ReadOnlySpan<byte> originalHmtx,
        ushort originalNumberOfHMetrics,
        List<int> sortedOldGlyphIds)
    {
        int numSubsetGlyphs = sortedOldGlyphIds.Count;
        var data = new byte[numSubsetGlyphs * 4];
        var advanceWidths = new ushort[numSubsetGlyphs];

        for (int i = 0; i < numSubsetGlyphs; i++)
        {
            int oldGid = sortedOldGlyphIds[i];
            ushort aw;
            short lsb;

            if (oldGid < originalNumberOfHMetrics)
            {
                int offset = oldGid * 4;
                if (offset + 4 <= originalHmtx.Length)
                {
                    aw = BinaryPrimitives.ReadUInt16BigEndian(originalHmtx[offset..]);
                    lsb = BinaryPrimitives.ReadInt16BigEndian(originalHmtx[(offset + 2)..]);
                }
                else
                {
                    aw = 0;
                    lsb = 0;
                }
            }
            else
            {
                // Beyond numberOfHMetrics: reuse last advance width
                int lastOffset = (originalNumberOfHMetrics - 1) * 4;
                aw = lastOffset + 2 <= originalHmtx.Length
                    ? BinaryPrimitives.ReadUInt16BigEndian(originalHmtx[lastOffset..])
                    : (ushort)0;

                // LSB from the leftSideBearing array
                int lsbArrayStart = originalNumberOfHMetrics * 4;
                int lsbIndex = oldGid - originalNumberOfHMetrics;
                int lsbOffset = lsbArrayStart + lsbIndex * 2;
                lsb = lsbOffset + 2 <= originalHmtx.Length
                    ? BinaryPrimitives.ReadInt16BigEndian(originalHmtx[lsbOffset..])
                    : (short)0;
            }

            advanceWidths[i] = aw;
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(i * 4), aw);
            BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(i * 4 + 2), lsb);
        }

        return (data, advanceWidths);
    }
}
