using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordTemplateProcessor.web.Enum;
using WordTemplateProcessor.web.Interfaces;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace WordTemplateProcessor.web.Services;

public class PlaceholderExtractor : IPlaceholderExtractor
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{(.*?)\}\}", RegexOptions.Compiled);

    public Dictionary<string, FieldType> ExtractPlaceholders(Stream docxStream)
    {
        var result = new Dictionary<string, FieldType>();

        using var wordDoc = WordprocessingDocument.Open(docxStream, false);
        var body = wordDoc.MainDocumentPart.Document.Body;

        var fullText = string.Join("", body.Descendants<Text>().Select(t => t.Text));
        foreach (Match match in PlaceholderRegex.Matches(fullText))
            result[match.Groups[1].Value.Trim()] = FieldType.Text;

        foreach (var sdt in body.Descendants<SdtElement>())
        {
            var sdtProps = sdt.Elements<SdtProperties>().FirstOrDefault();
            if (sdtProps == null) continue;

            var isImageControl = sdtProps.Elements<SdtContentPicture>().Any();
            if (!isImageControl) continue;

            var tag = sdtProps.Elements<Tag>().FirstOrDefault()?.Val?.Value;
            if (string.IsNullOrWhiteSpace(tag)) continue;

            var hasDrawing = sdt.Descendants<Drawing>().Any();
            if (hasDrawing) result[tag] = FieldType.Image;
        }

        return result;
    }
}