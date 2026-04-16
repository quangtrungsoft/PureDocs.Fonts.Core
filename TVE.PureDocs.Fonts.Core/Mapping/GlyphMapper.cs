// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Tables.CmapTable;

namespace TVE.PureDocs.Fonts.Core.Mapping;

/// <summary>
/// Maps Unicode codepoint → GlyphID. Immutable, thread-safe.
/// Parsed from cmap table (format 4 for BMP, format 12 for SMP).
/// </summary>
public sealed class GlyphMapper
{
    private readonly CmapTable _cmap;
    private readonly ReadOnlyMemory<byte> _glyfData;
    private readonly Tables.LocaTable? _loca;

    internal GlyphMapper(CmapTable cmap, Tables.LocaTable? loca, ReadOnlyMemory<byte> glyfData)
    {
        _cmap = cmap;
        _loca = loca;
        _glyfData = glyfData;
    }

    /// <summary>
    /// Lookup GlyphID. Returns 0 (.notdef) if codepoint not in font.
    /// O(log segCount) — binary search on cmap segments.
    /// </summary>
    public int GetGlyphId(int codepoint) => _cmap.GetGlyphId(codepoint);

    /// <summary>
    /// Bulk lookup — used when warming up FontMetricsCache.
    /// glyphIds.Length must be >= chars.Length.
    /// </summary>
    public void GetGlyphIds(ReadOnlySpan<char> chars, Span<int> glyphIds)
    {
        for (int i = 0; i < chars.Length; i++)
            glyphIds[i] = _cmap.GetGlyphId(chars[i]);
    }

    /// <summary>
    /// Build GlyphSet from chars.
    /// Automatically expands composite glyphs (components are added to set).
    /// Always includes glyphId=0 (.notdef).
    /// </summary>
    public GlyphSet BuildGlyphSet(IEnumerable<char> chars)
    {
        var set = new GlyphSet();
        foreach (var c in chars)
        {
            int gid = _cmap.GetGlyphId(c);
            if (gid > 0) set.Add(gid);
        }

        ExpandComposites(set);
        return set;
    }

    /// <summary>Overload for codepoints (surrogate pairs, SMP).</summary>
    public GlyphSet BuildGlyphSet(IEnumerable<int> codepoints)
    {
        var set = new GlyphSet();
        foreach (var cp in codepoints)
        {
            int gid = _cmap.GetGlyphId(cp);
            if (gid > 0) set.Add(gid);
        }

        ExpandComposites(set);
        return set;
    }

    /// <summary>
    /// Build GlyphSet from ShapedGlyphs (after DirectWrite shaping).
    /// GlyphIds are already determined by DirectWrite — no additional lookup.
    /// Still need to expand composites.
    /// </summary>
    public GlyphSet BuildGlyphSetFromShaped(IEnumerable<int> shapedGlyphIds)
    {
        var set = new GlyphSet();
        foreach (var gid in shapedGlyphIds)
            set.Add(gid);

        ExpandComposites(set);
        return set;
    }

    /// <summary>Gets the covered Unicode ranges from the cmap segments.</summary>
    public IReadOnlyList<(int Start, int End)> CoveredRanges
    {
        get
        {
            var ranges = new List<(int, int)>(_cmap.Segments.Length);
            foreach (var seg in _cmap.Segments)
                ranges.Add((seg.StartCode, seg.EndCode));
            return ranges;
        }
    }

    private void ExpandComposites(GlyphSet set)
    {
        if (_loca == null || _glyfData.IsEmpty) return;

        var resolver = new CompositeGlyphResolver(_loca, _glyfData);
        resolver.ExpandComposites(set);
    }
}
