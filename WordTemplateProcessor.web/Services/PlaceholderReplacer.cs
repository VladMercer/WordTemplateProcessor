using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlPowerTools;
using WordTemplateProcessor.web.Interfaces;
using Paragraph = DocumentFormat.OpenXml.Drawing.Paragraph;
using Run = DocumentFormat.OpenXml.Drawing.Run;
using Text = DocumentFormat.OpenXml.Drawing.Text;

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

            // Текстовые замены
            foreach (var (key, value) in fields)
            {
                if (key.EndsWith(":img")) continue;

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

            // Сохраняем результат текстовой замены
            mainPart.SaveXDocument();

            // Замена изображений
            foreach (var (key, imageBytes) in imageFields)
            {
                var placeholder = "{{" + key + "}}";
                _logger.LogInformation("Обработка плейсхолдера изображения: {Placeholder}", placeholder);

                foreach (var paragraph in mainPart.Document.Body.Descendants<Paragraph>())
                {
                    var paragraphText = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
                    if (!paragraphText.Contains(placeholder)) continue;

                    _logger.LogInformation("Найден параграф с изображением: {Text}", paragraphText);

                    // Удаляем все Run'ы внутри параграфа (вместе с плейсхолдером)
                    paragraph.RemoveAllChildren<Run>();

                    // Добавляем Run с изображением
                    var run = new Run();
                    var imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
                    using var imgStream = new MemoryStream(imageBytes);
                    imagePart.FeedData(imgStream);
                    var imageId = mainPart.GetIdOfPart(imagePart);

                    var drawing = CreateImageDrawing(imageId, key, 990000, 792000);
                    run.Append(drawing);
                    paragraph.Append(run);

                    _logger.LogInformation("Изображение {Key} вставлено в документ.", key);
                    break; // Предполагается один плейсхолдер на документ, иначе уберите break
                }
            }

            mainPart.Document.Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке шаблона Word.");
            throw;
        }

        outputStream.Position = 0;
        return outputStream;
    }

    private Drawing CreateImageDrawing(string relationshipId, string name, long cx, long cy)
    {
        return new Drawing(
            new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = cx, Cy = cy },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent()
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L
                },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties()
                {
                    Id = (UInt32Value)1U,
                    Name = name
                },
                new DocumentFormat.OpenXml.Drawing.Graphic(
                    new DocumentFormat.OpenXml.Drawing.GraphicData(
                            new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties()
                                    {
                                        Id = (UInt32Value)0U,
                                        Name = name
                                    },
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()
                                ),
                                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                    new DocumentFormat.OpenXml.Drawing.Blip()
                                    {
                                        Embed = relationshipId,
                                        CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print
                                    },
                                    new DocumentFormat.OpenXml.Drawing.Stretch(
                                        new DocumentFormat.OpenXml.Drawing.FillRectangle()
                                    )
                                ),
                                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                                        new DocumentFormat.OpenXml.Drawing.Offset() { X = 0L, Y = 0L },
                                        new DocumentFormat.OpenXml.Drawing.Extents() { Cx = cx, Cy = cy }
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                            new DocumentFormat.OpenXml.Drawing.AdjustValueList()
                                        )
                                        { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
            {
                DistanceFromTop = (UInt32Value)0U,
                DistanceFromBottom = (UInt32Value)0U,
                DistanceFromLeft = (UInt32Value)0U,
                DistanceFromRight = (UInt32Value)0U
            }
        );
    }
}