// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Parsing;
using TVE.PureDocs.Fonts.Core.Subsetting;

namespace TVE.PureDocs.Fonts.Core.Diagnostics;

/// <summary>
/// Verifies that a font subset contains all requested glyphs and is structurally valid.
/// </summary>
public static class SubsetVerifier
{
    /// <summary>
    /// Verifies a SubsetResult. Returns a list of issues found.
    /// Empty list = valid subset.
    /// </summary>
    public static IReadOnlyList<string> Verify(SubsetResult subset)
    {
        var issues = new List<string>();

        // 1. Basic checks
        if (subset.SubsetBytes.IsEmpty)
        {
            issues.Add("Subset bytes are empty.");
            return issues;
        }

        if (subset.SubsetGlyphCount <= 0)
            issues.Add($"SubsetGlyphCount is {subset.SubsetGlyphCount} (expected > 0).");

        // 2. Verify the subset is a valid font
        var fontIssues = TtfValidator.Validate(subset.SubsetBytes);
        foreach (var issue in fontIssues)
            issues.Add($"Subset font: {issue}");

        // 3. Verify glyph ID remap consistency
        if (subset.GlyphIdRemap.Count != subset.SubsetGlyphCount)
            issues.Add($"GlyphIdRemap has {subset.GlyphIdRemap.Count} entries " +
                       $"but SubsetGlyphCount is {subset.SubsetGlyphCount}.");

        // Verify new IDs are contiguous 0..N-1
        var newIds = new HashSet<int>(subset.GlyphIdRemap.Values);
        for (int i = 0; i < subset.SubsetGlyphCount; i++)
        {
            if (!newIds.Contains(i))
                issues.Add($"Missing new glyph ID {i} in remap (expected 0..{subset.SubsetGlyphCount - 1}).");
        }

        // 4. Verify .notdef (glyph 0) is included
        if (!subset.GlyphIdRemap.ContainsKey(0))
            issues.Add("GlyphIdRemap does not contain .notdef (glyph ID 0).");

        // 5. Verify CharToNewGlyphId values are valid
        foreach (var (c, newGid) in subset.CharToNewGlyphId)
        {
            if (newGid < 0 || newGid >= subset.SubsetGlyphCount)
                issues.Add($"CharToNewGlyphId['{c}' (U+{(int)c:X4})] = {newGid} " +
                           $"is out of range [0, {subset.SubsetGlyphCount}).");
        }

        // 6. Verify PrefixedFontName format
        if (string.IsNullOrEmpty(subset.PrefixedFontName))
            issues.Add("PrefixedFontName is empty.");
        else if (!subset.PrefixedFontName.Contains('+'))
            issues.Add($"PrefixedFontName '{subset.PrefixedFontName}' does not contain '+' separator.");

        return issues;
    }

    /// <summary>
    /// Verifies that all requested characters are present in the subset.
    /// </summary>
    public static IReadOnlyList<string> VerifyCharacters(SubsetResult subset, IEnumerable<char> requestedChars)
    {
        var issues = new List<string>();

        foreach (var c in requestedChars)
        {
            if (!subset.CharToNewGlyphId.ContainsKey(c))
                issues.Add($"Character '{c}' (U+{(int)c:X4}) not found in subset.");
        }

        return issues;
    }
}
