using WordTemplateProcessor.web.Services;

namespace WordTemplateProcessor.web.Requests;

public class FillTemplateRequest
{
    public IFormFile Template { get; set; }
    public List<FormField> Fields { get; set; }
}

public class FormField
{
    public string Key { get; set; }
    public FieldType Type { get; set; }
    public string Value { get; set; }
    public IFormFile File { get; set; }
}