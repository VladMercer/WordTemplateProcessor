using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WordTemplateProcessor.web.Interfaces;
using WordTemplateProcessor.web.Requests;
using WordTemplateProcessor.web.Services;

namespace WordTemplateProcessor.web.Controllers;

[ApiController]
[Route("[controller]")]
public class TemplateController : ControllerBase
{
    private readonly IPlaceholderExtractor _placeholderExtractor;
    private readonly IPlaceholderReplacer _placeholderReplacer;
    private readonly IPdfConverter _pdfConverter;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        IPlaceholderExtractor placeholderExtractor,
        IPlaceholderReplacer placeholderReplacer,
        IPdfConverter pdfConverter, ILogger<TemplateController> logger)
    {
        _placeholderExtractor = placeholderExtractor;
        _placeholderReplacer = placeholderReplacer;
        _pdfConverter = pdfConverter;
        _logger = logger;
    }

    [HttpPost("parse-template")]
    public IActionResult ParseTemplate(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var fields = _placeholderExtractor.ExtractPlaceholders(stream);
        return Ok(fields);
    }

    [HttpPost("fill-template")]
    public async Task<IActionResult> FillTemplate([FromForm] FillTemplateRequest request)
    {
        // Разделение полей на текстовые и изображения
        var textFields = request.Fields
            .Where(f => f.Type == FieldType.Text)
            .ToDictionary(f => f.Key, f => f.Value);

        var imageFields = request.Fields
            .Where(f => f.Type == FieldType.Image && f.File != null)
            .ToDictionary(f => f.Key, f =>
            {
                using var ms = new MemoryStream();
                f.File.CopyTo(ms);
                return ms.ToArray();
            });

        // Обработка документа
        await using var templateStream = request.Template.OpenReadStream();
        var filledDocx = _placeholderReplacer.ReplacePlaceholders(templateStream, textFields, imageFields);
        var pdfBytes = await _pdfConverter.ConvertToPdfAsync(filledDocx);

        return File(pdfBytes, "application/pdf", "filled.pdf");
    }
}