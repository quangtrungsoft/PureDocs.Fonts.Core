// Licensed to TVE under the MIT License.

using System.Text;

namespace TVE.PureDocs.Fonts.Core.Cmap;

/// <summary>
/// Builds ToUnicode CMap stream (ISO 32000-2 §9.10.3).
/// Required for PDF/A and text search/copy in PDF viewers.
///
/// Output is raw string — Pdf.Fonts compresses (FlateDecode) and embeds as PdfStream.
///
/// Supports:
///   - Single codepoint glyphs: &lt;glyphId_hex&gt; &lt;unicode_hex&gt;
///   - Ligature glyphs: &lt;glyphId_hex&gt; &lt;codepoint1 codepoint2 ...&gt;
///     (bfchar with UTF-16BE multi-codepoint strings)
/// </summary>
public static class ToUnicodeCMapBuilder
{
    /// <summary>Build from SubsetResult — preferred overload.</summary>
    public static string Build(Subsetting.SubsetResult subset, string cmapName = "Custom-UTF16")
    {
        // Prefer glyph→codepoints (handles ligatures)
        if (subset.GlyphToCodepoints.Count > 0)
            return Build(subset.GlyphToCodepoints, cmapName);

        // Fallback: char→glyphId
        if (subset.CharToNewGlyphId.Count > 0)
            return Build(subset.CharToNewGlyphId, cmapName);

        return BuildEmpty(cmapName);
    }

    /// <summary>Build from char→glyphId map (simple case, no ligatures).</summary>
    public static string Build(
        IReadOnlyDictionary<char, int> charToGlyphId,
        string cmapName = "Custom-UTF16")
    {
        // Invert: glyphId → unicode codepoint
        var glyphToCodepoints = new Dictionary<int, ReadOnlyMemory<int>>();
        foreach (var (c, gid) in charToGlyphId)
        {
            glyphToCodepoints[gid] = new[] { (int)c };
        }

        return Build(glyphToCodepoints, cmapName);
    }

    /// <summary>
    /// Build with ligature support.
    /// glyphToCodepoints: new glyphId → Unicode codepoints array
    /// </summary>
    public static string Build(
        IReadOnlyDictionary<int, ReadOnlyMemory<int>> glyphToCodepoints,
        string cmapName = "Custom-UTF16")
    {
        if (glyphToCodepoints.Count == 0)
            return BuildEmpty(cmapName);

        var sb = new StringBuilder(2048);

        // CMap header
        sb.AppendLine("/CIDInit /ProcSet findresource begin");
        sb.AppendLine("12 dict begin");
        sb.AppendLine("begincmap");
        sb.AppendLine("/CIDSystemInfo");
        sb.AppendLine("<< /Registry (Adobe)");
        sb.AppendLine("/Ordering (UCS)");
        sb.AppendLine("/Supplement 0");
        sb.AppendLine(">> def");
        sb.Append("/CMapName /").Append(cmapName).AppendLine(" def");
        sb.AppendLine("/CMapType 2 def");
        sb.AppendLine("1 begincodespacerange");
        sb.AppendLine("<0000> <FFFF>");
        sb.AppendLine("endcodespacerange");

        // Sort entries by glyph ID
        var entries = new List<KeyValuePair<int, ReadOnlyMemory<int>>>(glyphToCodepoints);
        entries.Sort((a, b) => a.Key.CompareTo(b.Key));

        // Write bfchar mappings in batches of 100 (PDF spec limit)
        int offset = 0;
        while (offset < entries.Count)
        {
            int batchSize = Math.Min(100, entries.Count - offset);
            sb.Append(batchSize).AppendLine(" beginbfchar");

            for (int i = 0; i < batchSize; i++)
            {
                var entry = entries[offset + i];
                sb.Append('<').Append(entry.Key.ToString("X4")).Append("> <");

                // Encode codepoints as UTF-16BE
                var codepoints = entry.Value.Span;
                for (int j = 0; j < codepoints.Length; j++)
                {
                    int cp = codepoints[j];
                    if (cp <= 0xFFFF)
                    {
                        sb.Append(cp.ToString("X4"));
                    }
                    else
                    {
                        // Supplementary plane: encode as UTF-16 surrogate pair
                        int adjusted = cp - 0x10000;
                        int hi = 0xD800 + (adjusted >> 10);
                        int lo = 0xDC00 + (adjusted & 0x3FF);
                        sb.Append(hi.ToString("X4")).Append(lo.ToString("X4"));
                    }
                }

                sb.AppendLine(">");
            }

            sb.AppendLine("endbfchar");
            offset += batchSize;
        }

        // CMap footer
        sb.AppendLine("endcmap");
        sb.AppendLine("CMapName currentdict /CMap defineresource pop");
        sb.AppendLine("end");
        sb.AppendLine("end");

        return sb.ToString();
    }

    private static string BuildEmpty(string cmapName)
    {
        var sb = new StringBuilder(512);
        sb.AppendLine("/CIDInit /ProcSet findresource begin");
        sb.AppendLine("12 dict begin");
        sb.AppendLine("begincmap");
        sb.AppendLine("/CIDSystemInfo");
        sb.AppendLine("<< /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def");
        sb.Append("/CMapName /").Append(cmapName).AppendLine(" def");
        sb.AppendLine("/CMapType 2 def");
        sb.AppendLine("1 begincodespacerange");
        sb.AppendLine("<0000> <FFFF>");
        sb.AppendLine("endcodespacerange");
        sb.AppendLine("endcmap");
        sb.AppendLine("CMapName currentdict /CMap defineresource pop");
        sb.AppendLine("end");
        sb.AppendLine("end");
        return sb.ToString();
    }
}
