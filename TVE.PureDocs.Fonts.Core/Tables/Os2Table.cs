// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'OS/2' table — OS/2 and Windows specific metrics.
/// Contains weight class, CapHeight, xHeight, fsSelection flags, etc.
/// </summary>
internal sealed class Os2Table
{
    /// <summary>Visual weight (degree of blackness) 1–1000. 400 = Regular, 700 = Bold.</summary>
    public ushort UsWeightClass { get; }

    /// <summary>fsSelection flags. Bit 0 = Italic, Bit 5 = Bold, Bit 6 = Regular.</summary>
    public ushort FsSelection { get; }

    /// <summary>Typographic ascender (sTypoAscender).</summary>
    public short STypoAscender { get; }

    /// <summary>Typographic descender (sTypoDescender), typically negative.</summary>
    public short STypoDescender { get; }

    /// <summary>Typographic line gap (sTypoLineGap).</summary>
    public short STypoLineGap { get; }

    /// <summary>Cap height (sCapHeight). Version 2+ only. 0 if not available.</summary>
    public short SCapHeight { get; }

    /// <summary>x-Height (sxHeight). Version 2+ only. 0 if not available.</summary>
    public short SXHeight { get; }

    /// <summary>Strikeout position (yStrikeoutPosition) in font units.</summary>
    public short YStrikeoutPosition { get; }

    /// <summary>Strikeout size (yStrikeoutSize) in font units.</summary>
    public short YStrikeoutSize { get; }

    /// <summary>True if fsSelection bit 5 is set.</summary>
    public bool IsBold => (FsSelection & 0x0020) != 0;

    /// <summary>True if fsSelection bit 0 is set.</summary>
    public bool IsItalic => (FsSelection & 0x0001) != 0;

    /// <summary>Minimum size of OS/2 table (78 bytes for version 0).</summary>
    public const int MinSize = 78;

    private Os2Table(ushort usWeightClass, ushort fsSelection,
        short sTypoAscender, short sTypoDescender, short sTypoLineGap,
        short sCapHeight, short sxHeight, short yStrikeoutPosition, short yStrikeoutSize)
    {
        UsWeightClass = usWeightClass;
        FsSelection = fsSelection;
        STypoAscender = sTypoAscender;
        STypoDescender = sTypoDescender;
        STypoLineGap = sTypoLineGap;
        SCapHeight = sCapHeight;
        SXHeight = sxHeight;
        YStrikeoutPosition = yStrikeoutPosition;
        YStrikeoutSize = yStrikeoutSize;
    }

    /// <summary>Parses the OS/2 table from raw table data.</summary>
    public static Os2Table Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
            throw new Parsing.InvalidFontException("OS/2 table too short.");

        var version = BinaryPrimitives.ReadUInt16BigEndian(data);
        var usWeightClass = BinaryPrimitives.ReadUInt16BigEndian(data[4..]);
        var fsSelection = BinaryPrimitives.ReadUInt16BigEndian(data[62..]);
        var sTypoAscender = BinaryPrimitives.ReadInt16BigEndian(data[68..]);
        var sTypoDescender = BinaryPrimitives.ReadInt16BigEndian(data[70..]);
        var sTypoLineGap = BinaryPrimitives.ReadInt16BigEndian(data[72..]);
        var yStrikeoutSize = BinaryPrimitives.ReadInt16BigEndian(data[26..]);
        var yStrikeoutPosition = BinaryPrimitives.ReadInt16BigEndian(data[28..]);

        // sCapHeight and sxHeight available in version 2+
        short sCapHeight = 0;
        short sxHeight = 0;
        if (version >= 2 && data.Length >= 90)
        {
            sxHeight = BinaryPrimitives.ReadInt16BigEndian(data[86..]);
            sCapHeight = BinaryPrimitives.ReadInt16BigEndian(data[88..]);
        }

        return new Os2Table(usWeightClass, fsSelection, sTypoAscender, sTypoDescender,
            sTypoLineGap, sCapHeight, sxHeight, yStrikeoutPosition, yStrikeoutSize);
    }
}
