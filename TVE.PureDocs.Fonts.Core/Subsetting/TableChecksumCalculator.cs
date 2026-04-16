// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Subsetting;

/// <summary>
/// Calculates table checksums for TTF font files.
/// Handles the special 0xB1B0AFBA adjustment for the 'head' table.
/// </summary>
internal static class TableChecksumCalculator
{
    /// <summary>
    /// Calculates the checksum of a table's data.
    /// Data is treated as uint32 big-endian words. Padded with zeros if not aligned.
    /// </summary>
    public static uint Calculate(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        int fullWords = data.Length / 4;

        for (int i = 0; i < fullWords; i++)
            sum += BinaryPrimitives.ReadUInt32BigEndian(data[(i * 4)..]);

        // Handle remaining bytes (pad with zeros)
        int remaining = data.Length % 4;
        if (remaining > 0)
        {
            Span<byte> padded = stackalloc byte[4];
            padded.Clear();
            data[(fullWords * 4)..].CopyTo(padded);
            sum += BinaryPrimitives.ReadUInt32BigEndian(padded);
        }

        return sum;
    }

    /// <summary>
    /// Calculates head.checkSumAdjustment for the entire font file.
    /// checkSumAdjustment = 0xB1B0AFBA - sum_of_entire_file
    /// The head table's checkSumAdjustment field must be zeroed before calculating.
    /// </summary>
    public static uint CalculateHeadCheckSumAdjustment(byte[] fontFile, int headTableOffset)
    {
        // Zero out checkSumAdjustment (offset 8 within head table)
        int adjOffset = headTableOffset + 8;
        byte b0 = fontFile[adjOffset], b1 = fontFile[adjOffset + 1],
            b2 = fontFile[adjOffset + 2], b3 = fontFile[adjOffset + 3];

        fontFile[adjOffset] = fontFile[adjOffset + 1] =
            fontFile[adjOffset + 2] = fontFile[adjOffset + 3] = 0;

        uint fileChecksum = Calculate(fontFile);
        uint adjustment = 0xB1B0AFBA - fileChecksum;

        // Write the adjustment value
        BinaryPrimitives.WriteUInt32BigEndian(fontFile.AsSpan(adjOffset), adjustment);

        return adjustment;
    }
}
