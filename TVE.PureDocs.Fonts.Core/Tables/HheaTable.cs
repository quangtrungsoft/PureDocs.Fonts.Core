// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'hhea' table — Horizontal header. Contains vertical metrics and numberOfHMetrics.
/// </summary>
internal sealed class HheaTable
{
    /// <summary>Typographic ascender (font units).</summary>
    public short Ascender { get; }

    /// <summary>Typographic descender (font units, typically negative).</summary>
    public short Descender { get; }

    /// <summary>Typographic line gap (font units).</summary>
    public short LineGap { get; }

    /// <summary>Maximum advance width (font units).</summary>
    public ushort AdvanceWidthMax { get; }

    /// <summary>
    /// Number of hMetric entries in 'hmtx' table.
    /// Glyphs beyond this index reuse the last advance width.
    /// </summary>
    public ushort NumberOfHMetrics { get; }

    /// <summary>Minimum size of hhea table (36 bytes).</summary>
    public const int MinSize = 36;

    private HheaTable(short ascender, short descender, short lineGap,
        ushort advanceWidthMax, ushort numberOfHMetrics)
    {
        Ascender = ascender;
        Descender = descender;
        LineGap = lineGap;
        AdvanceWidthMax = advanceWidthMax;
        NumberOfHMetrics = numberOfHMetrics;
    }

    /// <summary>Parses the hhea table from raw table data.</summary>
    public static HheaTable Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
            throw new Parsing.InvalidFontException("hhea table too short.");

        return new HheaTable(
            ascender: BinaryPrimitives.ReadInt16BigEndian(data[4..]),
            descender: BinaryPrimitives.ReadInt16BigEndian(data[6..]),
            lineGap: BinaryPrimitives.ReadInt16BigEndian(data[8..]),
            advanceWidthMax: BinaryPrimitives.ReadUInt16BigEndian(data[10..]),
            numberOfHMetrics: BinaryPrimitives.ReadUInt16BigEndian(data[34..])
        );
    }
}
