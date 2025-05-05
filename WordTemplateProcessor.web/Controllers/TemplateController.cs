using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WordTemplateProcessor.web.Interfaces;

namespace WordTemplateProcessor.web.Controllers;

[ApiController]
[Route("[controller]")]
public class TemplateController : ControllerBase
{
    private readonly ILogger<TemplateController> _logger;
    private readonly IPdfConverter _pdfConverter;
    private readonly IPlaceholderExtractor _placeholderExtractor;
    private readonly IPlaceholderReplacer _placeholderReplacer;

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
    public async Task<IActionResult> FillTemplate()
    {
        var form = Request.Form;
        var template = form.Files["Template"];
        var fieldsJson = form["Fields"];

        var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(fieldsJson!);
        
        var textFields = fields
            .Where(kvp => !form.Files.Any(f => f.Name == kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        var imageFields = form.Files
            .Where(f => f.Name != "Template" && f.Length > 0)
            .GroupBy(f => f.Name)
            .ToDictionary(g => g.Key, g =>
            {
                using var ms = new MemoryStream();
                g.First().CopyTo(ms);
                return ms.ToArray();
            });

        await using var templateStream = template.OpenReadStream();
        var filledDocx = _placeholderReplacer.ReplacePlaceholders(templateStream, textFields, imageFields);
        var pdfBytes = await _pdfConverter.ConvertToPdfAsync(filledDocx);

        return File(pdfBytes, "application/pdf", "filled.pdf");
    }
}