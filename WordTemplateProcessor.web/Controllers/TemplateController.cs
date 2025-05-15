using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WordTemplateProcessor.web.Interfaces;

namespace WordTemplateProcessor.web.Controllers;

[ApiController]
[Route("[controller]")]
public class TemplateController : ControllerBase
{
    private readonly IPdfConverter _pdfConverter;
    private readonly IPlaceholderExtractor _placeholderExtractor;
    private readonly IPlaceholderReplacer _placeholderReplacer;

    public TemplateController(
        IPlaceholderExtractor placeholderExtractor,
        IPlaceholderReplacer placeholderReplacer,
        IPdfConverter pdfConverter)
    {
        _placeholderExtractor = placeholderExtractor;
        _placeholderReplacer = placeholderReplacer;
        _pdfConverter = pdfConverter;
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
        Dictionary<string, string> textFields;
        textFields = JsonSerializer.Deserialize<Dictionary<string, string>>(fieldsJson!) ??
                     new Dictionary<string, string>();

        await using var templateStream = template.OpenReadStream();
        var filledDocx = await _placeholderReplacer
            .ReplacePlaceholders(templateStream, textFields);

        filledDocx.Position = 0;
        var pdfBytes = await _pdfConverter.ConvertToPdfAsync(filledDocx);

        return File(pdfBytes, "application/pdf", "filled.pdf");
    }
}