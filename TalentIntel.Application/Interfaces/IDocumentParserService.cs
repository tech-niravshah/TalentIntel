using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TalentIntel.Application.Interfaces;

/// <summary>
/// Extracts text content and skills from uploaded resume/JD files.
/// </summary>
public interface IDocumentParserService
{
    /// <summary>
    /// Extracts raw text from a PDF or DOCX file stream.
    /// Throws UnsupportedFormatException for other formats.
    /// </summary>
    Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans raw text for known technical skills using keyword matching.
    /// Returns a distinct lowercase list of matched skills.
    /// </summary>
    List<string> ExtractSkills(string rawText);
}
