// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'post' table — PostScript name mapping. Contains italic angle and underline metrics.
/// </summary>
internal sealed class PostTable
{
    /// <summary>Italic angle in degrees, counter-clockwise from vertical. 0 for upright.</summary>
    public float ItalicAngle { get; }

    /// <summary>Underline position (font units, typically negative).</summary>
    public short UnderlinePosition { get; }

    /// <summary>Underline thickness (font units).</summary>
    public short UnderlineThickness { get; }

    /// <summary>0 if proportionally spaced, non-zero if monospaced.</summary>
    public uint IsFixedPitch { get; }

    /// <summary>Minimum size of post table (32 bytes).</summary>
    public const int MinSize = 32;

    private PostTable(float italicAngle, short underlinePosition,
        short underlineThickness, uint isFixedPitch)
    {
        ItalicAngle = italicAngle;
        UnderlinePosition = underlinePosition;
        UnderlineThickness = underlineThickness;
        IsFixedPitch = isFixedPitch;
    }

    /// <summary>Parses the post table from raw table data.</summary>
    public static PostTable Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
            throw new Parsing.InvalidFontException("post table too short.");

        // italicAngle is a Fixed 16.16 value at offset 4
        int fixedAngle = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
        float italicAngle = fixedAngle / 65536.0f;

        var underlinePosition = BinaryPrimitives.ReadInt16BigEndian(data[8..]);
        var underlineThickness = BinaryPrimitives.ReadInt16BigEndian(data[10..]);
        var isFixedPitch = BinaryPrimitives.ReadUInt32BigEndian(data[12..]);

        return new PostTable(italicAngle, underlinePosition, underlineThickness, isFixedPitch);
    }
}
