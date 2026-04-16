// Licensed to TVE under the MIT License.

namespace TVE.PureDocs.Fonts.Core.Subsetting;

/// <summary>
/// CFF/OTF subsetter stub. CFF subsetting is significantly more complex than TrueType.
/// This stub provides minimal support for CFF-based fonts.
///
/// Full CFF subsetting involves: charstrings, subroutine resolution,
/// local/global subrs, CFF2 variable fonts, etc.
/// </summary>
internal static class CffSubsetter
{
    /// <summary>
    /// Creates a minimal CFF subset. Currently returns the full CFF data as-is.
    /// TODO: Implement proper CFF charstring subsetting with subroutine resolution.
    /// </summary>
    public static byte[] Subset(ReadOnlySpan<byte> cffData, IReadOnlyCollection<int> glyphIds)
    {
        // For now, return the full CFF table data.
        // CFF subsetting is complex and will be implemented in a later phase.
        // Most PDF viewers handle full CFF data correctly even if not subsetted.
        return cffData.ToArray();
    }
}
