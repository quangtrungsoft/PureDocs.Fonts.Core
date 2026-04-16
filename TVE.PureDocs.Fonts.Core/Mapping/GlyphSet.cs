// Licensed to TVE under the MIT License.

namespace TVE.PureDocs.Fonts.Core.Mapping;

/// <summary>
/// Set of glyph IDs. Always includes glyph 0 (.notdef).
/// Used as input to TtfSubsetter.
/// </summary>
public sealed class GlyphSet
{
    private readonly HashSet<int> _glyphIds = new() { 0 }; // Always include .notdef

    /// <summary>Number of glyphs in the set (including .notdef).</summary>
    public int Count => _glyphIds.Count;

    /// <summary>Adds a glyph ID to the set.</summary>
    public void Add(int glyphId) => _glyphIds.Add(glyphId);

    /// <summary>Checks if the set contains a glyph ID.</summary>
    public bool Contains(int glyphId) => _glyphIds.Contains(glyphId);

    /// <summary>Returns a sorted list of all glyph IDs.</summary>
    public List<int> ToSortedList()
    {
        var list = new List<int>(_glyphIds);
        list.Sort();
        return list;
    }

    /// <summary>Returns all glyph IDs as a read-only collection.</summary>
    public IReadOnlyCollection<int> GlyphIds => _glyphIds;

    /// <summary>Adds all glyph IDs from another set.</summary>
    public void UnionWith(GlyphSet other)
    {
        foreach (var gid in other._glyphIds)
            _glyphIds.Add(gid);
    }
}
