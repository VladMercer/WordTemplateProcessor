using WordTemplateProcessor.web.Enum;

namespace WordTemplateProcessor.web.Models;

public class FormField
{
    public string Key { get; set; }
    public FieldType Type { get; set; }
    public string Value { get; set; }
    public IFormFile File { get; set; }
}