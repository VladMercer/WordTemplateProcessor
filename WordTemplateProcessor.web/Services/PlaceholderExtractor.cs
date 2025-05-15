using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using WordTemplateProcessor.web.Interfaces;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace WordTemplateProcessor.web.Services;

public class PlaceholderExtractor : IPlaceholderExtractor
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{(.*?)\}\}", RegexOptions.Compiled);

    public IList<string> ExtractPlaceholders(Stream docxStream)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var wordDoc = WordprocessingDocument.Open(docxStream, false);
        var body = wordDoc.MainDocumentPart.Document.Body;

        var fullText = string.Join("", body.Descendants<Text>().Select(t => t.Text));

        foreach (Match match in PlaceholderRegex.Matches(fullText))
        {
            var name = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(name))
                set.Add(name);
        }

        return set.ToList();
    }
}