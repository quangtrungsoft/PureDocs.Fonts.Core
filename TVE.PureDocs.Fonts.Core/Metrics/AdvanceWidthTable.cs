// Licensed to TVE under the MIT License.

using System.Runtime.CompilerServices;

namespace TVE.PureDocs.Fonts.Core.Metrics;

/// <summary>
/// Advance widths from hmtx table. Immutable, thread-safe.
///
/// Units:
///   GetNormalized(glyphId) → 0.0..2.0+ (relative units, / unitsPerEm)
///     → * fontSizePt = pt (used in RenderEngine layout)
///   GetRaw(glyphId) → font units
///     → * 1000 / UPM = 1/1000 em (used in PDF /Widths array)
///
/// Note: AdvanceWidthTable is raw TTF data (no hinting).
///   RenderEngine uses IGlyphMeasurer (DirectWrite, with hinting) for layout.
///   AdvanceWidthTable is used for: Pdf.Fonts /Widths array, initial warmup seed.
/// </summary>
public sealed class AdvanceWidthTable
{
    private readonly ushort[] _rawWidths;
    private readonly int _unitsPerEm;

    /// <summary>Number of glyphs with width data.</summary>
    public int GlyphCount => _rawWidths.Length;

    /// <summary>Units per em from head table.</summary>
    public int UnitsPerEm => _unitsPerEm;

    internal AdvanceWidthTable(ushort[] rawWidths, int unitsPerEm)
    {
        _rawWidths = rawWidths;
        _unitsPerEm = unitsPerEm;
    }

    /// <summary>
    /// Gets the normalized advance width (/ unitsPerEm) for a glyph ID.
    /// Result * fontSizePt = width in points.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetNormalized(int glyphId)
    {
        int idx = Math.Min(glyphId, _rawWidths.Length - 1);
        if (idx < 0) idx = 0;
        return (float)_rawWidths[idx] / _unitsPerEm;
    }

    /// <summary>
    /// Gets the raw advance width in font units for a glyph ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetRaw(int glyphId)
    {
        int idx = Math.Min(glyphId, _rawWidths.Length - 1);
        if (idx < 0) idx = 0;
        return _rawWidths[idx];
    }

    /// <summary>
    /// Build PDF /Widths array (1/1000 em) for range [firstGlyphId, lastGlyphId].
    /// Used in TrueTypeFont/CidFontType2 dict construction.
    /// </summary>
    public int[] BuildPdfWidthsArray(int firstGlyphId, int lastGlyphId)
    {
        int count = lastGlyphId - firstGlyphId + 1;
        if (count <= 0) return Array.Empty<int>();

        var widths = new int[count];
        for (int i = 0; i < count; i++)
        {
            int gid = firstGlyphId + i;
            widths[i] = GetRaw(gid) * 1000 / _unitsPerEm;
        }

        return widths;
    }

    /// <summary>
    /// Default width (DW) for Type0/CIDFont — mode width of all glyphs.
    /// PDF /DW value = mode * 1000 / UPM.
    /// </summary>
    public int GetDefaultPdfWidth()
    {
        if (_rawWidths.Length == 0) return 0;

        // Find mode (most common width)
        var histogram = new Dictionary<ushort, int>();
        foreach (var w in _rawWidths)
        {
            histogram.TryGetValue(w, out int count);
            histogram[w] = count + 1;
        }

        ushort modeWidth = 0;
        int maxCount = 0;
        foreach (var (width, count) in histogram)
        {
            if (count > maxCount)
            {
                maxCount = count;
                modeWidth = width;
            }
        }

        return modeWidth * 1000 / _unitsPerEm;
    }
}
