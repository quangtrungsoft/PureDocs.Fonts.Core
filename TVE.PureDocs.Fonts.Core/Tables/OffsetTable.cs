// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// Offset Table — the very first structure in a TTF/OTF file.
/// Contains the sfVersion and the number of table records.
/// </summary>
internal readonly struct OffsetTable
{
    /// <summary>0x00010000 for TrueType, 'OTTO' (0x4F54544F) for CFF.</summary>
    public uint SfVersion { get; }

    /// <summary>Number of tables in the font.</summary>
    public ushort NumTables { get; }

    public ushort SearchRange { get; }
    public ushort EntrySelector { get; }
    public ushort RangeShift { get; }

    /// <summary>Size of the Offset Table in bytes (12).</summary>
    public const int Size = 12;

    private OffsetTable(uint sfVersion, ushort numTables, ushort searchRange,
        ushort entrySelector, ushort rangeShift)
    {
        SfVersion = sfVersion;
        NumTables = numTables;
        SearchRange = searchRange;
        EntrySelector = entrySelector;
        RangeShift = rangeShift;
    }

    /// <summary>Parses the Offset Table from the beginning of font data.</summary>
    public static OffsetTable Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
            throw new Parsing.InvalidFontException("Font data too short for Offset Table.");

        return new OffsetTable(
            sfVersion: BinaryPrimitives.ReadUInt32BigEndian(data),
            numTables: BinaryPrimitives.ReadUInt16BigEndian(data[4..]),
            searchRange: BinaryPrimitives.ReadUInt16BigEndian(data[6..]),
            entrySelector: BinaryPrimitives.ReadUInt16BigEndian(data[8..]),
            rangeShift: BinaryPrimitives.ReadUInt16BigEndian(data[10..])
        );
    }

    /// <summary>True if this is a CFF-based OpenType font ('OTTO').</summary>
    public bool IsCff => SfVersion == 0x4F54544F;

    /// <summary>True if this is a TrueType font (version 1.0).</summary>
    public bool IsTrueType => SfVersion == 0x00010000;
}
