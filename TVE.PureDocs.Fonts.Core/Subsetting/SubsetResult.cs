// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Metrics;

namespace TVE.PureDocs.Fonts.Core.Subsetting;

/// <summary>
/// Output of font subsetting operation.
/// Contains the subset font bytes and all mapping information needed for PDF embedding.
/// </summary>
public sealed class SubsetResult
{
    /// <summary>Valid TTF/OTF subset bytes — ready to embed in PDF /FontFile2.</summary>
    public ReadOnlyMemory<byte> SubsetBytes { get; init; }

    /// <summary>
    /// Old glyphId → new glyphId after reindexing.
    /// Pdf.Fonts uses this to build /CIDToGIDMap stream.
    /// </summary>
    public IReadOnlyDictionary<int, int> GlyphIdRemap { get; init; } = new Dictionary<int, int>();

    /// <summary>
    /// Char (Unicode) → new glyphId in subset.
    /// Used to build ToUnicode CMap. Required for PDF/A.
    /// </summary>
    public IReadOnlyDictionary<char, int> CharToNewGlyphId { get; init; } = new Dictionary<char, int>();

    /// <summary>
    /// Codepoint sequences → new glyphId (for ligatures).
    /// Key: glyphId. Value: codepoints that glyph represents.
    /// Used to build ToUnicode CMap for ligatures.
    /// </summary>
    public IReadOnlyDictionary<int, ReadOnlyMemory<int>> GlyphToCodepoints { get; init; } =
        new Dictionary<int, ReadOnlyMemory<int>>();

    /// <summary>Number of glyphs in the subset.</summary>
    public int SubsetGlyphCount { get; init; }

    /// <summary>AdvanceWidthTable indexed by new glyph IDs.</summary>
    public AdvanceWidthTable? SubsetAdvanceWidths { get; init; }

    /// <summary>Font name prefix per PDF spec: "ABCDEF+FontName" (6 uppercase random chars).</summary>
    public string PrefixedFontName { get; init; } = "";
}
