// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'maxp' table — Maximum profile. Contains numGlyphs.
/// </summary>
internal sealed class MaxpTable
{
    /// <summary>Total number of glyphs in the font.</summary>
    public ushort NumGlyphs { get; }

    /// <summary>Table version (1.0 for TrueType, 0.5 for CFF).</summary>
    public uint Version { get; }

    /// <summary>Minimum size of maxp table (6 bytes for version 0.5).</summary>
    public const int MinSize = 6;

    private MaxpTable(uint version, ushort numGlyphs)
    {
        Version = version;
        NumGlyphs = numGlyphs;
    }

    /// <summary>Parses the maxp table from raw table data.</summary>
    public static MaxpTable Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
            throw new Parsing.InvalidFontException("maxp table too short.");

        var version = BinaryPrimitives.ReadUInt32BigEndian(data);
        var numGlyphs = BinaryPrimitives.ReadUInt16BigEndian(data[4..]);

        if (numGlyphs == 0)
            throw new Parsing.InvalidFontException("maxp.numGlyphs is zero.");

        return new MaxpTable(version, numGlyphs);
    }
}
