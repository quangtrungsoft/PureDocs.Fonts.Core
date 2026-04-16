// Licensed to TVE under the MIT License.

using System.Buffers.Binary;
using System.Text;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'name' table — Naming table. Contains font family name, subfamily, full name, etc.
/// </summary>
internal sealed class NameTable
{
    /// <summary>Font family name (nameId = 1).</summary>
    public string FamilyName { get; }

    /// <summary>Font subfamily name (nameId = 2): "Regular", "Bold", "Italic", "Bold Italic".</summary>
    public string SubfamilyName { get; }

    /// <summary>Full font name (nameId = 4), e.g. "Arial Bold".</summary>
    public string FullName { get; }

    /// <summary>PostScript name (nameId = 6), e.g. "ArialMT".</summary>
    public string PostScriptName { get; }

    private NameTable(string familyName, string subfamilyName, string fullName, string postScriptName)
    {
        FamilyName = familyName;
        SubfamilyName = subfamilyName;
        FullName = fullName;
        PostScriptName = postScriptName;
    }

    /// <summary>Parses the name table from raw table data.</summary>
    public static NameTable Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 6)
            throw new Parsing.InvalidFontException("name table too short.");

        var count = BinaryPrimitives.ReadUInt16BigEndian(data[2..]);
        var storageOffset = BinaryPrimitives.ReadUInt16BigEndian(data[4..]);

        string familyName = "";
        string subfamilyName = "";
        string fullName = "";
        string postScriptName = "";

        int recordOffset = 6;

        for (int i = 0; i < count && recordOffset + 12 <= data.Length; i++)
        {
            var platformId = BinaryPrimitives.ReadUInt16BigEndian(data[recordOffset..]);
            var encodingId = BinaryPrimitives.ReadUInt16BigEndian(data[(recordOffset + 2)..]);
            // var languageId = BinaryPrimitives.ReadUInt16BigEndian(data[(recordOffset + 4)..]);
            var nameId = BinaryPrimitives.ReadUInt16BigEndian(data[(recordOffset + 6)..]);
            var length = BinaryPrimitives.ReadUInt16BigEndian(data[(recordOffset + 8)..]);
            var stringOffset = BinaryPrimitives.ReadUInt16BigEndian(data[(recordOffset + 10)..]);

            recordOffset += 12;

            // Only interested in nameIds 1, 2, 4, 6
            if (nameId is not (1 or 2 or 4 or 6)) continue;

            int start = storageOffset + stringOffset;
            if (start + length > data.Length) continue;

            var stringData = data.Slice(start, length);
            var text = DecodeNameString(stringData, platformId, encodingId);

            if (string.IsNullOrEmpty(text)) continue;

            switch (nameId)
            {
                case 1 when string.IsNullOrEmpty(familyName):
                    familyName = text;
                    break;
                case 2 when string.IsNullOrEmpty(subfamilyName):
                    subfamilyName = text;
                    break;
                case 4 when string.IsNullOrEmpty(fullName):
                    fullName = text;
                    break;
                case 6 when string.IsNullOrEmpty(postScriptName):
                    postScriptName = text;
                    break;
            }
        }

        return new NameTable(
            familyName: familyName.Length > 0 ? familyName : "Unknown",
            subfamilyName: subfamilyName.Length > 0 ? subfamilyName : "Regular",
            fullName: fullName,
            postScriptName: postScriptName
        );
    }

    private static string DecodeNameString(ReadOnlySpan<byte> data, ushort platformId, ushort encodingId)
    {
        // Platform 3 (Windows) or Platform 0 (Unicode): UTF-16 Big-Endian
        if (platformId is 3 or 0)
        {
            return Encoding.BigEndianUnicode.GetString(data);
        }

        // Platform 1 (Macintosh): MacRoman (approximate as ASCII/Latin1)
        if (platformId == 1)
        {
            return Encoding.ASCII.GetString(data);
        }

        return Encoding.UTF8.GetString(data);
    }
}
