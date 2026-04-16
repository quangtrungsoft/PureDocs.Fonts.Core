// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables.CmapTable;

/// <summary>
/// CMap Format 12 reader — Segmented coverage.
/// Covers full Unicode range including supplementary planes (U+10000+).
///
/// Structure:
///   format(2) reserved(2) length(4) language(4) numGroups(4)
///   groups[numGroups]:
///     startCharCode(4) endCharCode(4) startGlyphID(4)
/// </summary>
internal static class CmapFormat12Reader
{
    private const int HeaderSize = 16;
    private const int GroupSize = 12;

    public static CmapSegment[] Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize)
            throw new Parsing.InvalidFontException("cmap format 12 subtable too short.");

        var numGroups = (int)BinaryPrimitives.ReadUInt32BigEndian(data[12..]);

        if (numGroups == 0)
            return Array.Empty<CmapSegment>();

        var segments = new CmapSegment[numGroups];

        for (int i = 0; i < numGroups; i++)
        {
            int off = HeaderSize + i * GroupSize;
            if (off + GroupSize > data.Length)
                throw new Parsing.InvalidFontException("cmap format 12 group data truncated.");

            var startCharCode = (int)BinaryPrimitives.ReadUInt32BigEndian(data[off..]);
            var endCharCode = (int)BinaryPrimitives.ReadUInt32BigEndian(data[(off + 4)..]);
            var startGlyphId = (int)BinaryPrimitives.ReadUInt32BigEndian(data[(off + 8)..]);

            // Delta = startGlyphId - startCharCode
            // glyphId = codepoint + delta
            int delta = startGlyphId - startCharCode;
            segments[i] = new CmapSegment(startCharCode, endCharCode, delta);
        }

        return segments;
    }
}
