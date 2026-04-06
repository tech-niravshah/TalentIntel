using System;

namespace TalentIntel.Infrastructure.Exceptions;

/// <summary>
/// Thrown when a document upload contains a file format that cannot be parsed.
/// Supported formats: PDF, DOCX.
/// </summary>
public class UnsupportedFormatException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedFormatException"/> class.
    /// </summary>
    public UnsupportedFormatException(string fileName)
        : base($"File format not supported: {fileName}. Supported formats: PDF, DOCX.")
    {
    }
}
