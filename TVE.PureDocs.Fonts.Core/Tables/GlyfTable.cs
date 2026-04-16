// Licensed to TVE under the MIT License.

using System.Buffers.Binary;

namespace TVE.PureDocs.Fonts.Core.Tables;

/// <summary>
/// 'glyf' table — Glyph data. Contains glyph outlines (simple and composite).
/// This class provides methods to inspect composite glyph flags and component IDs.
/// </summary>
internal static class GlyfTable
{
    // Composite glyph flags (ISO/IEC 14496-22 §5.3.4)
    private const ushort ARG_1_AND_2_ARE_WORDS = 0x0001;
    private const ushort WE_HAVE_A_SCALE = 0x0008;
    private const ushort MORE_COMPONENTS = 0x0020;
    private const ushort WE_HAVE_AN_X_AND_Y_SCALE = 0x0040;
    private const ushort WE_HAVE_A_TWO_BY_TWO = 0x0080;
    private const ushort WE_HAVE_INSTRUCTIONS = 0x0100;

    /// <summary>
    /// Determines if the glyph at the given data span is composite (numberOfContours &lt; 0).
    /// </summary>
    public static bool IsComposite(ReadOnlySpan<byte> glyphData)
    {
        if (glyphData.Length < 2) return false;
        var numberOfContours = BinaryPrimitives.ReadInt16BigEndian(glyphData);
        return numberOfContours < 0;
    }

    /// <summary>
    /// Extracts all component glyph IDs from a composite glyph.
    /// Used for expanding composite dependencies during subsetting.
    /// </summary>
    public static List<ushort> GetCompositeComponents(ReadOnlySpan<byte> glyphData)
    {
        var components = new List<ushort>();

        if (glyphData.Length < 10) return components;

        // Skip glyph header (10 bytes): numberOfContours(2) + xMin(2) + yMin(2) + xMax(2) + yMax(2)
        int offset = 10;
        bool hasMore = true;

        while (hasMore && offset + 4 <= glyphData.Length)
        {
            ushort flags = BinaryPrimitives.ReadUInt16BigEndian(glyphData[offset..]);
            ushort componentGlyphId = BinaryPrimitives.ReadUInt16BigEndian(glyphData[(offset + 2)..]);
            components.Add(componentGlyphId);
            offset += 4;

            // Skip arguments (translation/offset)
            if ((flags & ARG_1_AND_2_ARE_WORDS) != 0)
                offset += 4; // two int16
            else
                offset += 2; // two int8

            // Skip transformation matrix components
            if ((flags & WE_HAVE_A_SCALE) != 0)
                offset += 2; // one F2Dot14
            else if ((flags & WE_HAVE_AN_X_AND_Y_SCALE) != 0)
                offset += 4; // two F2Dot14
            else if ((flags & WE_HAVE_A_TWO_BY_TWO) != 0)
                offset += 8; // four F2Dot14

            hasMore = (flags & MORE_COMPONENTS) != 0;
        }

        return components;
    }

    /// <summary>
    /// Patches composite glyph data: rewrites component glyph IDs using the remap dictionary.
    /// Returns a new byte array with patched component IDs.
    /// </summary>
    public static byte[] PatchCompositeGlyphIds(
        ReadOnlySpan<byte> glyphData,
        IReadOnlyDictionary<int, int> glyphIdRemap)
    {
        var result = glyphData.ToArray();
        int offset = 10; // skip glyph header
        bool hasMore = true;

        while (hasMore && offset + 4 <= result.Length)
        {
            ushort flags = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(offset));
            ushort oldId = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(offset + 2));

            // Patch the component glyph ID
            if (glyphIdRemap.TryGetValue(oldId, out var newId))
            {
                BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset + 2), (ushort)newId);
            }

            offset += 4;

            if ((flags & ARG_1_AND_2_ARE_WORDS) != 0)
                offset += 4;
            else
                offset += 2;

            if ((flags & WE_HAVE_A_SCALE) != 0)
                offset += 2;
            else if ((flags & WE_HAVE_AN_X_AND_Y_SCALE) != 0)
                offset += 4;
            else if ((flags & WE_HAVE_A_TWO_BY_TWO) != 0)
                offset += 8;

            hasMore = (flags & MORE_COMPONENTS) != 0;
        }

        return result;
    }
}
