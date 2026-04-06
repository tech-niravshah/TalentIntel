using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using TalentIntel.Application.Interfaces;
using TalentIntel.Infrastructure.Exceptions;
using UglyToad.PdfPig;

namespace TalentIntel.Infrastructure.Parsing;

/// <summary>
/// Parses resume and JD files to extract raw text and skills.
///
/// Supported formats:
///   - PDF  → UglyToad.PdfPig: PdfDocument.Open(byte[])
///             extract text via: doc.GetPages() → page.Text
///   - DOCX → DocumentFormat.OpenXml: WordprocessingDocument.Open(stream, false)
///             extract text via: body.Descendants<Text>().Select(t => t.Text)
///
/// Skill extraction uses a curated keyword list — a simple but effective
/// approach for cold-start scenarios where no historical match data exists.
/// </summary>
public class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentParserService"/> class.
    /// </summary>
    public DocumentParserService(ILogger<DocumentParserService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Known technical skills for keyword extraction.
    /// All entries must be lowercase — comparison is case-insensitive.
    /// </summary>
    private static readonly HashSet<string> KnownSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "c#", "dotnet", ".net", "python", "java", "javascript", "typescript",
        "react", "angular", "vue", "node", "sql", "postgresql", "mongodb",
        "azure", "aws", "gcp", "docker", "kubernetes", "terraform",
        "redis", "kafka", "rabbitmq", "graphql", "rest", "microservices",
        "git", "ci/cd", "devops", "machine learning", "deep learning",
        "pytorch", "tensorflow", "spark", "databricks"
    };

    /// <summary>
    /// Extracts raw text from a PDF or DOCX file stream.
    /// Throws UnsupportedFormatException for other formats.
    /// </summary>
    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting text from {FileName}", fileName);

        if (fileStream is null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension == ".pdf")
        {
            // PdfPig requires a byte[] input, so copy the stream first.
            await using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);

            using var document = PdfDocument.Open(memoryStream.ToArray());
            var text = string.Join(" ", document.GetPages().Select(page => page.Text));

            _logger.LogInformation("Extracted text from PDF {FileName}", fileName);
            return text;
        }

        if (extension == ".docx")
        {
            // OpenXML reads directly from the provided stream without modifying it.
            using var document = WordprocessingDocument.Open(fileStream, false);
            var body = document.MainDocumentPart?.Document?.Body;
            var text = body is null
                ? string.Empty
                : string.Join(" ", body.Descendants<Text>().Select(node => node.Text));

            _logger.LogInformation("Extracted text from DOCX {FileName}", fileName);
            return text;
        }

        _logger.LogInformation("Unsupported file extension {Extension}", extension);
        throw new UnsupportedFormatException(fileName);
    }

    /// <summary>
    /// Scans raw text for known technical skills using keyword matching.
    /// Returns a distinct lowercase list of matched skills.
    /// </summary>
    public List<string> ExtractSkills(string rawText)
    {
        _logger.LogInformation("Extracting skills from raw text");

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new List<string>();
        }

        // Normalize input to lowercase to simplify keyword matching.
        var lower = rawText.ToLowerInvariant();

        var matches = KnownSkills
            .Where(skill => lower.Contains(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(skill => skill.ToLowerInvariant())
            .ToList();

        _logger.LogInformation("Extracted {Count} skills", matches.Count);
        return matches;
    }
}
