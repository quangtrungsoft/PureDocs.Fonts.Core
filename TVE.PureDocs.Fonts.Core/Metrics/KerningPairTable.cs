// Licensed to TVE under the MIT License.

using System.Runtime.CompilerServices;

namespace TVE.PureDocs.Fonts.Core.Metrics;

/// <summary>
/// Kerning pair lookup from kern table. Immutable, thread-safe.
/// Optional — null if font has no kern table.
/// </summary>
public sealed class KerningPairTable
{
    private readonly Dictionary<(ushort Left, ushort Right), short> _rawPairs;
    private readonly int _unitsPerEm;

    /// <summary>Number of kerning pairs.</summary>
    public int Count => _rawPairs.Count;

    internal KerningPairTable(Dictionary<(ushort Left, ushort Right), short> rawPairs, int unitsPerEm)
    {
        _rawPairs = rawPairs;
        _unitsPerEm = unitsPerEm;
    }

    /// <summary>
    /// Gets the normalized kerning value for a pair of glyph IDs.
    /// Returns 0 if no kerning pair exists.
    /// Result * fontSizePt = kerning adjustment in points.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetNormalized(int leftGlyphId, int rightGlyphId)
    {
        if (_rawPairs.TryGetValue(((ushort)leftGlyphId, (ushort)rightGlyphId), out var value))
            return (float)value / _unitsPerEm;
        return 0f;
    }

    /// <summary>
    /// Gets the raw kerning value in font units.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetRaw(int leftGlyphId, int rightGlyphId)
    {
        if (_rawPairs.TryGetValue(((ushort)leftGlyphId, (ushort)rightGlyphId), out var value))
            return value;
        return 0;
    }

    /// <summary>
    /// Gets all kerning pairs as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<(ushort Left, ushort Right), short> RawPairs => _rawPairs;
}
