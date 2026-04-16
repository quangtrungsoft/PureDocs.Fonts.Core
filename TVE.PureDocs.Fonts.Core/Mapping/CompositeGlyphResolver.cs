// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Tables;

namespace TVE.PureDocs.Fonts.Core.Mapping;

/// <summary>
/// Resolves composite glyph dependencies from the glyf table.
/// Expands a GlyphSet to include all component glyph IDs referenced by composite glyphs.
/// </summary>
internal sealed class CompositeGlyphResolver
{
    private readonly LocaTable _loca;
    private readonly ReadOnlyMemory<byte> _glyfData;

    public CompositeGlyphResolver(LocaTable loca, ReadOnlyMemory<byte> glyfData)
    {
        _loca = loca;
        _glyfData = glyfData;
    }

    /// <summary>
    /// Expands the GlyphSet to include all components of composite glyphs.
    /// Uses BFS to handle nested composites.
    /// </summary>
    public void ExpandComposites(GlyphSet set)
    {
        var toCheck = new Queue<int>(set.GlyphIds);
        var visited = new HashSet<int>(set.GlyphIds);

        while (toCheck.Count > 0)
        {
            int glyphId = toCheck.Dequeue();
            var (start, end) = _loca.GetGlyphRange(glyphId);
            if (start >= end) continue;

            var span = _glyfData.Span;
            if (start + 2 > (uint)span.Length) continue;

            var glyphSlice = span[(int)start..(int)Math.Min(end, (uint)span.Length)];

            if (!GlyfTable.IsComposite(glyphSlice)) continue;

            var components = GlyfTable.GetCompositeComponents(glyphSlice);
            foreach (var compId in components)
            {
                set.Add(compId);
                if (visited.Add(compId))
                    toCheck.Enqueue(compId);
            }
        }
    }
}
