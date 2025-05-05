using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlPowerTools;
using WordTemplateProcessor.web.Interfaces;

namespace WordTemplateProcessor.web.Services;

public class PlaceholderReplacer : IPlaceholderReplacer
{
    private readonly ILogger<PlaceholderReplacer> _logger;

    public PlaceholderReplacer(ILogger<PlaceholderReplacer> logger)
    {
        _logger = logger;
    }

    public MemoryStream ReplacePlaceholders(
        Stream docxStream,
        Dictionary<string, string> fields,
        Dictionary<string, byte[]> imageFields)
    {
        var outputStream = new MemoryStream();
        docxStream.Position = 0;
        docxStream.CopyTo(outputStream);
        outputStream.Position = 0;

        try
        {
            using var wordDoc = WordprocessingDocument.Open(outputStream, true);
            var mainPart = wordDoc.MainDocumentPart;

            var xdoc = mainPart.GetXElement();
            
            foreach (var (key, value) in fields)
            {
                try
                {
                    var pattern = new Regex(Regex.Escape("{{" + key + "}}"));
                    var paragraphs = xdoc.Descendants(W.p);
                    OpenXmlRegex.Replace(paragraphs, pattern, value ?? "", null);
                    _logger.LogInformation("Заменён текстовый плейсхолдер: {{Key}}", key);
                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning(innerEx, "Ошибка при замене плейсхолдера: {Key}", key);
                }
            }
            
            mainPart.SaveXDocument();
            
            foreach (var (key, imageBytes) in imageFields)
            {
                var tag = key;

                var sdts = wordDoc.MainDocumentPart.Document.Body
                    .Descendants<OpenXmlElement>()
                    .Where(el =>
                        (el is SdtBlock || el is SdtRun) &&
                        el.Elements<SdtProperties>().FirstOrDefault()?.GetFirstChild<Tag>()?.Val == tag
                    );

                foreach (var sdt in sdts)
                {
                    var drawing = sdt.Descendants<Drawing>().FirstOrDefault();
                    if (drawing == null)
                    {
                        _logger.LogWarning("Не найден Drawing в SdtBlock для тега {Tag}", tag);
                        continue;
                    }

                    var blip = drawing.Descendants<Blip>().FirstOrDefault();
                    if (blip == null)
                    {
                        _logger.LogWarning("Не найден Blip в Drawing для тега {Tag}", tag);
                        continue;
                    }

                    var relId = blip.Embed?.Value;
                    if (string.IsNullOrEmpty(relId))
                    {
                        _logger.LogWarning("Пустой или отсутствующий Embed Id для тега {Tag}", tag);
                        continue;
                    }

                    var imagePart = (ImagePart)wordDoc.MainDocumentPart.GetPartById(relId);
                    using var imageStream = new MemoryStream(imageBytes);
                    imagePart.FeedData(imageStream);

                    _logger.LogInformation("Изображение успешно заменено для контент-контрола с тегом {Tag}", tag);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке шаблона Word.");
            throw;
        }

        outputStream.Position = 0;
        return outputStream;
    }
}