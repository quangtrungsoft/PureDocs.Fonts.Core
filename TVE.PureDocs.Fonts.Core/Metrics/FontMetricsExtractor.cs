// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Tables;

namespace TVE.PureDocs.Fonts.Core.Metrics;

/// <summary>
/// Extracts FontMetrics from parsed table data.
/// </summary>
internal static class FontMetricsExtractor
{
    /// <summary>Builds normalized FontMetrics from the parsed tables.</summary>
    public static FontMetrics Extract(HeadTable head, HheaTable hhea, Os2Table? os2, PostTable? post)
    {
        float upm = head.UnitsPerEm;

        // StemV heuristic from OS/2 weight class
        int stemV = 80; // default
        if (os2 != null)
        {
            int w = os2.UsWeightClass;
            stemV = (int)((w / 65.0) * (w / 65.0) + 50);
        }

        // PDF Flags bitmask
        int flags = 0;
        // Bit 1 (value 1): FixedPitch
        if (post?.IsFixedPitch != 0 && post != null)
            flags |= (1 << 0);
        // Bit 3 (value 4): Symbolic — always set for TrueType embedded fonts
        flags |= (1 << 2);
        // Bit 6 (value 32): Nonsymbolic — set for Latin fonts
        // Bit 7 (value 64): Italic
        if (os2?.IsItalic == true || head.IsItalic)
            flags |= (1 << 6);

        return new FontMetrics
        {
            UnitsPerEm = head.UnitsPerEm,
            Ascender = hhea.Ascender / upm,
            Descender = hhea.Descender / upm,
            LineGap = hhea.LineGap / upm,
            CapHeight = os2 != null ? os2.SCapHeight / upm : hhea.Ascender * 0.7f / upm,
            XHeight = os2 != null ? os2.SXHeight / upm : hhea.Ascender * 0.5f / upm,
            ItalicAngle = post?.ItalicAngle ?? 0f,
            StemV = stemV,
            Flags = flags,
            BBoxXMin = head.XMin / upm,
            BBoxYMin = head.YMin / upm,
            BBoxXMax = head.XMax / upm,
            BBoxYMax = head.YMax / upm,
            UnderlinePosition = post != null ? post.UnderlinePosition / upm : -0.1f,
            UnderlineThickness = post != null ? post.UnderlineThickness / upm : 0.05f,
            StrikeoutPosition = os2 != null ? os2.YStrikeoutPosition / upm : 0.3f,
            StrikeoutThickness = os2 != null ? os2.YStrikeoutSize / upm : 0.05f,
        };
    }
}
