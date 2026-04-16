// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'loca' table — Index to location. Maps glyph IDs to offsets within 'glyf' table.
/// Depends on head.indexToLocFormat (0 = short, 1 = long) and maxp.numGlyphs.
/// </summary>
internal sealed class LocaTable
{
    /// <summary>Offsets into the 'glyf' table for each glyph. Length = numGlyphs + 1.</summary>
    public uint[] Offsets { get; }

    private LocaTable(uint[] offsets)
    {
        Offsets = offsets;
    }

    /// <summary>
    /// Parses the loca table.
    /// </summary>
    /// <param name="data">Raw loca table data.</param>
    /// <param name="indexToLocFormat">0 = short (uint16 × 2), 1 = long (uint32).</param>
    /// <param name="numGlyphs">From maxp table.</param>
    public static LocaTable Parse(ReadOnlySpan<byte> data, short indexToLocFormat, ushort numGlyphs)
    {
        int count = numGlyphs + 1; // loca has numGlyphs + 1 entries
        var offsets = new uint[count];

        if (indexToLocFormat == 0)
        {
            // Short format: uint16 values, multiply by 2 to get actual offsets
            int required = count * 2;
            if (data.Length < required)
                throw new Parsing.InvalidFontException("loca table (short format) too short.");

            for (int i = 0; i < count; i++)
                offsets[i] = (uint)(BinaryPrimitives.ReadUInt16BigEndian(data[(i * 2)..]) * 2);
        }
        else
        {
            // Long format: uint32 values
            int required = count * 4;
            if (data.Length < required)
                throw new Parsing.InvalidFontException("loca table (long format) too short.");

            for (int i = 0; i < count; i++)
                offsets[i] = BinaryPrimitives.ReadUInt32BigEndian(data[(i * 4)..]);
        }

        return new LocaTable(offsets);
    }

    /// <summary>
    /// Gets the byte range [start, end) of a glyph within the 'glyf' table.
    /// If start == end, the glyph has no outline (space, empty glyph).
    /// </summary>
    public (uint Start, uint End) GetGlyphRange(int glyphId)
    {
        if (glyphId < 0 || glyphId >= Offsets.Length - 1)
            return (0, 0);
        return (Offsets[glyphId], Offsets[glyphId + 1]);
    }
}
