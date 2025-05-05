using WordTemplateProcessor.web.Models;

namespace WordTemplateProcessor.web.Requests;

public class FillTemplateRequest
{
    public IFormFile Template { get; set; }
    public List<FormField> Fields { get; set; }
}