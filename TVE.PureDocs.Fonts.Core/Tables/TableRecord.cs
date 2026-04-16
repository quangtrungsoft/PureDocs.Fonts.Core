// Licensed to TVE under the MIT License.

using System.Buffers.Binary;
using System.Text;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// Table Record — describes one table's location and checksum within the font file.
/// Each record is 16 bytes. Located immediately after the Offset Table.
/// </summary>
internal readonly struct TableRecord
{
    /// <summary>4-byte ASCII tag identifying the table (e.g. "head", "glyf", "CFF ").</summary>
    public string Tag { get; }

    /// <summary>Table checksum.</summary>
    public uint Checksum { get; }

    /// <summary>Offset from the beginning of the font file to the table data.</summary>
    public uint Offset { get; }

    /// <summary>Length of the table data in bytes.</summary>
    public uint Length { get; }

    /// <summary>Size of one TableRecord in bytes (16).</summary>
    public const int Size = 16;

    private TableRecord(string tag, uint checksum, uint offset, uint length)
    {
        Tag = tag;
        Checksum = checksum;
        Offset = offset;
        Length = length;
    }

    /// <summary>Parses a single TableRecord from the given span.</summary>
    public static TableRecord Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
            throw new Parsing.InvalidFontException("Data too short for TableRecord.");

        var tagBytes = data[..4];
        var tag = Encoding.ASCII.GetString(tagBytes);

        return new TableRecord(
            tag: tag,
            checksum: BinaryPrimitives.ReadUInt32BigEndian(data[4..]),
            offset: BinaryPrimitives.ReadUInt32BigEndian(data[8..]),
            length: BinaryPrimitives.ReadUInt32BigEndian(data[12..])
        );
    }

    /// <summary>
    /// Parses all table records from font data (starting after the Offset Table).
    /// </summary>
    public static TableRecord[] ParseAll(ReadOnlySpan<byte> data, int numTables)
    {
        var records = new TableRecord[numTables];
        var offset = OffsetTable.Size;

        for (int i = 0; i < numTables; i++)
        {
            records[i] = Parse(data[offset..]);
            offset += Size;
        }

        return records;
    }
}
