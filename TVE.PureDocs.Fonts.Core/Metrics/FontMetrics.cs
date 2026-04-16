// Licensed to TVE under the MIT License.

using System.Runtime.CompilerServices;

namespace TVE.PureDocs.Fonts.Core.Metrics;

/// <summary>
/// Normalized font metrics (all values / unitsPerEm).
/// To get points: value * fontSizePt.
///
/// Used by:
///   RenderEngine/Layout: GetLineHeightPt, GetBaselineOffsetPt
///   Pdf.Fonts/FontDescriptor: Ascent, Descent, CapHeight, StemV, FontBBox, ItalicAngle
/// </summary>
public readonly record struct FontMetrics
{
    /// <summary>head.unitsPerEm (2048 common for TrueType, 1000 for CFF).</summary>
    public int UnitsPerEm { get; init; }

    // Vertical metrics (from hhea — used for line layout)

    /// <summary>hhea.ascender / UPM (positive).</summary>
    public float Ascender { get; init; }

    /// <summary>hhea.descender / UPM (negative).</summary>
    public float Descender { get; init; }

    /// <summary>hhea.lineGap / UPM.</summary>
    public float LineGap { get; init; }

    // PDF FontDescriptor fields

    /// <summary>OS/2.sCapHeight / UPM.</summary>
    public float CapHeight { get; init; }

    /// <summary>OS/2.sxHeight / UPM.</summary>
    public float XHeight { get; init; }

    /// <summary>post.italicAngle (degrees, CCW positive).</summary>
    public float ItalicAngle { get; init; }

    /// <summary>OS/2 weight heuristic: (usWeightClass/65)^2 + 50.</summary>
    public int StemV { get; init; }

    /// <summary>PDF FontDescriptor /Flags bitmask.</summary>
    public int Flags { get; init; }

    // Bounding box (normalized, PDF FontDescriptor /FontBBox)

    /// <summary>Global bounding box xMin / UPM.</summary>
    public float BBoxXMin { get; init; }

    /// <summary>Global bounding box yMin / UPM.</summary>
    public float BBoxYMin { get; init; }

    /// <summary>Global bounding box xMax / UPM.</summary>
    public float BBoxXMax { get; init; }

    /// <summary>Global bounding box yMax / UPM.</summary>
    public float BBoxYMax { get; init; }

    // Underline / Strikethrough (from post + OS/2)

    /// <summary>post.underlinePosition / UPM (typically negative).</summary>
    public float UnderlinePosition { get; init; }

    /// <summary>post.underlineThickness / UPM.</summary>
    public float UnderlineThickness { get; init; }

    /// <summary>OS/2.yStrikeoutPosition / UPM.</summary>
    public float StrikeoutPosition { get; init; }

    /// <summary>OS/2.yStrikeoutSize / UPM.</summary>
    public float StrikeoutThickness { get; init; }

    // === Convenience helpers ===

    /// <summary>Line height = (Ascender - Descender + LineGap) * sizePt.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetLineHeightPt(float sizePt)
        => (Ascender - Descender + LineGap) * sizePt;

    /// <summary>
    /// Distance from top of line box to baseline.
    /// baseline_y_from_top = Ascender * sizePt
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetBaselineOffsetPt(float sizePt)
        => Ascender * sizePt;

    /// <summary>PDF /FontBBox array: [xmin ymin xmax ymax] in 1/1000 em units.</summary>
    public int[] GetPdfFontBBox()
        => new[]
        {
            (int)(BBoxXMin * 1000), (int)(BBoxYMin * 1000),
            (int)(BBoxXMax * 1000), (int)(BBoxYMax * 1000)
        };
}
