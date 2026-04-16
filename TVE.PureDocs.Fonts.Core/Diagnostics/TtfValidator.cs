// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Parsing;

namespace TVE.PureDocs.Fonts.Core.Diagnostics;

/// <summary>
/// Validates TrueType/OpenType font binary data.
/// Checks required tables, checksums, and structural integrity.
/// </summary>
public static class TtfValidator
{
    /// <summary>Required tables for a valid TrueType font.</summary>
    private static readonly string[] RequiredTrueTypeTables =
    {
        "head", "hhea", "maxp", "cmap", "hmtx", "loca", "glyf", "name", "post"
    };

    /// <summary>Required tables for a valid CFF-based OpenType font.</summary>
    private static readonly string[] RequiredCffTables =
    {
        "head", "hhea", "maxp", "cmap", "hmtx", "name", "post", "CFF "
    };

    /// <summary>
    /// Validates font data and returns a list of issues found.
    /// Empty list = valid font.
    /// </summary>
    public static IReadOnlyList<string> Validate(ReadOnlyMemory<byte> fontData)
    {
        var issues = new List<string>();

        try
        {
            var span = fontData.Span;

            if (span.Length < 12)
            {
                issues.Add("Font data too short (less than 12 bytes).");
                return issues;
            }

            var offsetTable = Tables.OffsetTable.Parse(span);

            if (!offsetTable.IsTrueType && !offsetTable.IsCff)
            {
                issues.Add($"Unknown sfVersion: 0x{offsetTable.SfVersion:X8}.");
                return issues;
            }

            // Parse table records
            var records = Tables.TableRecord.ParseAll(span, offsetTable.NumTables);
            var tableNames = new HashSet<string>(records.Select(r => r.Tag));

            // Check required tables
            var required = offsetTable.IsCff ? RequiredCffTables : RequiredTrueTypeTables;
            foreach (var tag in required)
            {
                if (!tableNames.Contains(tag))
                    issues.Add($"Missing required table: '{tag}'.");
            }

            // Validate table offsets and lengths
            foreach (var rec in records)
            {
                if (rec.Offset + rec.Length > (uint)fontData.Length)
                    issues.Add($"Table '{rec.Tag}' extends beyond font data " +
                               $"(offset={rec.Offset}, length={rec.Length}, fileSize={fontData.Length}).");
            }

            // Validate checksums
            foreach (var rec in records)
            {
                if (rec.Tag == "head") continue; // head has special checksum handling
                if (rec.Offset + rec.Length > (uint)fontData.Length) continue;

                var tableData = fontData.Span.Slice((int)rec.Offset, (int)rec.Length);
                var checksum = Subsetting.TableChecksumCalculator.Calculate(tableData);
                if (checksum != rec.Checksum)
                    issues.Add($"Table '{rec.Tag}' checksum mismatch " +
                               $"(expected=0x{rec.Checksum:X8}, actual=0x{checksum:X8}).");
            }

            // Basic head table validation
            if (tableNames.Contains("head"))
            {
                var headRec = records.First(r => r.Tag == "head");
                var headData = fontData.Span.Slice((int)headRec.Offset, (int)headRec.Length);
                if (headData.Length >= 54)
                {
                    var head = Tables.HeadTable.Parse(headData);
                    if (head.UnitsPerEm < 16 || head.UnitsPerEm > 16384)
                        issues.Add($"head.unitsPerEm out of range: {head.UnitsPerEm} (valid: 16–16384).");
                }
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Validation error: {ex.Message}");
        }

        return issues;
    }

    /// <summary>
    /// Quick check: returns true if the font has all required tables.
    /// Does not check checksums.
    /// </summary>
    public static bool HasRequiredTables(TtfFontData font)
    {
        var required = font.IsCff ? RequiredCffTables : RequiredTrueTypeTables;
        return required.All(font.HasTable);
    }
}
