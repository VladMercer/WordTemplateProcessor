using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing;
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
        {
            result[match.Groups[1].Value.Trim()] = FieldType.Text;
        }

        foreach (var sdt in body.Descendants<SdtBlock>())
        {
            var sdtProperties = sdt.Elements<SdtProperties>().FirstOrDefault();
            if (sdtProperties == null) continue;

            var aliasElement = sdtProperties.Elements<SdtAlias>().FirstOrDefault();
            var alias = aliasElement?.Val?.Value;

            var isImageControl = sdtProperties.Elements<SdtContentPicture>().Any();

            if (!string.IsNullOrEmpty(alias) && isImageControl)
            {
                result[alias] = FieldType.Image;
            }
        }

        return result;
    }
}

public enum FieldType
{
    Text,
    Image
}