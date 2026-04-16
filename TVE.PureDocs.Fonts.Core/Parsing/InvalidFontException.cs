// Licensed to TVE under the MIT License.

namespace TVE.PureDocs.Fonts.Core.Parsing;

/// <summary>
/// Thrown when font binary data is invalid, corrupt, or missing required tables.
/// </summary>
public sealed class InvalidFontException : Exception
{
    public InvalidFontException(string message) : base(message) { }

    public InvalidFontException(string message, Exception innerException)
        : base(message, innerException) { }
}
