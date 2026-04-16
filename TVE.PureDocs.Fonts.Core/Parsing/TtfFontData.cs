// Licensed to TVE under the MIT License.

using TVE.PureDocs.Fonts.Core.Mapping;
using TVE.PureDocs.Fonts.Core.Metrics;
using TVE.PureDocs.Fonts.Core.Tables;
using TVE.PureDocs.Fonts.Core.Tables.CmapTable;

namespace TVE.PureDocs.Fonts.Core.Parsing;

/// <summary>
/// Immutable parsed font data. Thread-safe.
/// All consumers (Pdf.Fonts, RenderEngine) share the same instance.
/// </summary>
public sealed class TtfFontData
{
    // === Identity ===

    /// <summary>Font family name (name table, nameId=1).</summary>
    public string FamilyName { get; }

    /// <summary>Font subfamily name (nameId=2): "Regular"/"Bold"/"Italic"/"Bold Italic".</summary>
    public string SubfamilyName { get; }

    /// <summary>PostScript name (nameId=6).</summary>
    public string PostScriptName { get; }

    /// <summary>True if font is bold (OS/2.fsSelection bit 5 OR head.macStyle bit 0).</summary>
    public bool IsBold { get; }

    /// <summary>True if font is italic (OS/2.fsSelection bit 0 OR head.macStyle bit 1).</summary>
    public bool IsItalic { get; }

    /// <summary>True if font has CFF or CFF2 table (OpenType CFF font).</summary>
    public bool IsCff { get; }

    // === Metrics — normalized, thread-safe ===

    /// <summary>Normalized font metrics (all values / unitsPerEm).</summary>
    public FontMetrics Metrics { get; }

    /// <summary>Per-glyph advance widths.</summary>
    public AdvanceWidthTable AdvanceWidths { get; }

    /// <summary>Kerning pairs. Null if font has no kern table.</summary>
    public KerningPairTable? KerningPairs { get; }

    // === Mapping ===

    /// <summary>Unicode codepoint → GlyphID mapper.</summary>
    public GlyphMapper GlyphMapper { get; }

    // === Raw table access (for TtfSubsetter) ===

    private readonly Dictionary<string, ReadOnlyMemory<byte>> _rawTables;

    /// <summary>List of all table tags present in the font.</summary>
    public IReadOnlyList<string> TableTags { get; }

    /// <summary>
    /// Returns raw bytes of a table. Zero-copy slice from the original font memory.
    /// Tag: 4-char ASCII e.g. "glyf", "loca", "hmtx", "CFF ".
    /// Returns empty if table not found.
    /// </summary>
    public ReadOnlyMemory<byte> GetRawTable(string tag) =>
        _rawTables.TryGetValue(tag, out var data) ? data : ReadOnlyMemory<byte>.Empty;

    /// <summary>Returns true if the font contains the specified table.</summary>
    public bool HasTable(string tag) => _rawTables.ContainsKey(tag);

    /// <summary>Number of glyphs in the font.</summary>
    public int GlyphCount { get; }

    /// <summary>Units per em from head table.</summary>
    public int UnitsPerEm { get; }

    internal TtfFontData(
        string familyName, string subfamilyName, string postScriptName,
        bool isBold, bool isItalic, bool isCff,
        FontMetrics metrics, AdvanceWidthTable advanceWidths, KerningPairTable? kerningPairs,
        GlyphMapper glyphMapper,
        Dictionary<string, ReadOnlyMemory<byte>> rawTables,
        int glyphCount, int unitsPerEm)
    {
        FamilyName = familyName;
        SubfamilyName = subfamilyName;
        PostScriptName = postScriptName;
        IsBold = isBold;
        IsItalic = isItalic;
        IsCff = isCff;
        Metrics = metrics;
        AdvanceWidths = advanceWidths;
        KerningPairs = kerningPairs;
        GlyphMapper = glyphMapper;
        _rawTables = rawTables;
        TableTags = rawTables.Keys.ToList().AsReadOnly();
        GlyphCount = glyphCount;
        UnitsPerEm = unitsPerEm;
    }
}
