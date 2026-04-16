// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'head' table — Font header. Contains global font metrics.
/// ISO/IEC 14496-22 §5.2.3 (OpenType spec).
/// </summary>
internal sealed class HeadTable
{
    /// <summary>Units per em (typically 1000 for CFF, 2048 for TrueType).</summary>
    public ushort UnitsPerEm { get; }

    /// <summary>0 for short loca offsets (uint16), 1 for long (uint32).</summary>
    public short IndexToLocFormat { get; }

    /// <summary>Global font bounding box — xMin.</summary>
    public short XMin { get; }

    /// <summary>Global font bounding box — yMin.</summary>
    public short YMin { get; }

    /// <summary>Global font bounding box — xMax.</summary>
    public short XMax { get; }

    /// <summary>Global font bounding box — yMax.</summary>
    public short YMax { get; }

    /// <summary>macStyle flags: bit 0 = Bold, bit 1 = Italic.</summary>
    public ushort MacStyle { get; }

    /// <summary>True if macStyle bit 0 is set.</summary>
    public bool IsBold => (MacStyle & 0x0001) != 0;

    /// <summary>True if macStyle bit 1 is set.</summary>
    public bool IsItalic => (MacStyle & 0x0002) != 0;

    /// <summary>Minimum size of head table in bytes.</summary>
    public const int MinSize = 54;

    private HeadTable(ushort unitsPerEm, short indexToLocFormat,
        short xMin, short yMin, short xMax, short yMax, ushort macStyle)
    {
        UnitsPerEm = unitsPerEm;
        IndexToLocFormat = indexToLocFormat;
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
        MacStyle = macStyle;
    }

    /// <summary>Parses the head table from raw table data.</summary>
    public static HeadTable Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
            throw new Parsing.InvalidFontException("head table too short.");

        // Offset 18: unitsPerEm (uint16)
        var unitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(data[18..]);
        if (unitsPerEm == 0)
            throw new Parsing.InvalidFontException("head.unitsPerEm is zero.");

        // Offset 36: xMin, yMin, xMax, yMax (int16 each)
        var xMin = BinaryPrimitives.ReadInt16BigEndian(data[36..]);
        var yMin = BinaryPrimitives.ReadInt16BigEndian(data[38..]);
        var xMax = BinaryPrimitives.ReadInt16BigEndian(data[40..]);
        var yMax = BinaryPrimitives.ReadInt16BigEndian(data[42..]);

        // Offset 44: macStyle (uint16)
        var macStyle = BinaryPrimitives.ReadUInt16BigEndian(data[44..]);

        // Offset 50: indexToLocFormat (int16)
        var indexToLocFormat = BinaryPrimitives.ReadInt16BigEndian(data[50..]);

        return new HeadTable(unitsPerEm, indexToLocFormat, xMin, yMin, xMax, yMax, macStyle);
    }
}
