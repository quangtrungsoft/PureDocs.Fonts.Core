// Licensed to TVE under the MIT License.

using System.Buffers.Binary;
using TVE.PureDocs.Fonts.Core.Tables;

namespace TVE.PureDocs.Fonts.Core.Subsetting;

/// <summary>
/// Subsets glyf + loca tables: copies only the glyph data for included glyph IDs
/// and patches composite glyph component references.
/// </summary>
internal static class GlyfTableSubsetter
{
    /// <summary>
    /// Rebuilds glyf and loca tables for the subset.
    /// </summary>
    /// <param name="glyfData">Original glyf table data.</param>
    /// <param name="locaOffsets">Original loca offsets.</param>
    /// <param name="sortedNewGlyphs">Sorted list of old glyph IDs in the subset.</param>
    /// <param name="glyphIdRemap">Old glyph ID → new glyph ID mapping.</param>
    /// <param name="useShortLoca">Output: true if short loca format should be used.</param>
    /// <returns>Tuple of (glyfData, locaData).</returns>
    public static (byte[] GlyfData, byte[] LocaData) Build(
        ReadOnlySpan<byte> glyfData,
        uint[] locaOffsets,
        List<int> sortedNewGlyphs,
        IReadOnlyDictionary<int, int> glyphIdRemap,
        out bool useShortLoca)
    {
        int numSubsetGlyphs = sortedNewGlyphs.Count;
        var newLocaOffsets = new uint[numSubsetGlyphs + 1];

        // First pass: calculate total glyf size and collect glyph bytes
        var glyphChunks = new List<byte[]>(numSubsetGlyphs);
        uint currentOffset = 0;

        for (int i = 0; i < numSubsetGlyphs; i++)
        {
            int oldGlyphId = sortedNewGlyphs[i];
            newLocaOffsets[i] = currentOffset;

            if (oldGlyphId < locaOffsets.Length - 1)
            {
                uint start = locaOffsets[oldGlyphId];
                uint end = locaOffsets[oldGlyphId + 1];

                if (end > start && start < (uint)glyfData.Length)
                {
                    int len = (int)Math.Min(end - start, (uint)glyfData.Length - start);
                    var glyphSlice = glyfData.Slice((int)start, len);

                    byte[] chunk;
                    if (GlyfTable.IsComposite(glyphSlice))
                    {
                        // Patch composite glyph component IDs
                        chunk = GlyfTable.PatchCompositeGlyphIds(glyphSlice, glyphIdRemap);
                    }
                    else
                    {
                        chunk = glyphSlice.ToArray();
                    }

                    // Pad to 4-byte boundary for alignment
                    int padded = (chunk.Length + 3) & ~3;
                    if (padded > chunk.Length)
                        Array.Resize(ref chunk, padded);

                    glyphChunks.Add(chunk);
                    currentOffset += (uint)chunk.Length;
                    continue;
                }
            }

            // Empty glyph (no outline)
            glyphChunks.Add(Array.Empty<byte>());
        }

        newLocaOffsets[numSubsetGlyphs] = currentOffset;

        // Determine loca format: short if all offsets fit in uint16 * 2
        useShortLoca = currentOffset <= 0x1FFFE; // max value for short loca

        // Build glyf data
        var newGlyfData = new byte[currentOffset];
        int destOffset = 0;
        foreach (var chunk in glyphChunks)
        {
            chunk.CopyTo(newGlyfData, destOffset);
            destOffset += chunk.Length;
        }

        // Build loca data
        byte[] newLocaData;
        if (useShortLoca)
        {
            newLocaData = new byte[(numSubsetGlyphs + 1) * 2];
            for (int i = 0; i <= numSubsetGlyphs; i++)
                BinaryPrimitives.WriteUInt16BigEndian(newLocaData.AsSpan(i * 2), (ushort)(newLocaOffsets[i] / 2));
        }
        else
        {
            newLocaData = new byte[(numSubsetGlyphs + 1) * 4];
            for (int i = 0; i <= numSubsetGlyphs; i++)
                BinaryPrimitives.WriteUInt32BigEndian(newLocaData.AsSpan(i * 4), newLocaOffsets[i]);
        }

        return (newGlyfData, newLocaData);
    }
}
