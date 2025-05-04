using WordTemplateProcessor.web.Services;

namespace WordTemplateProcessor.web.Interfaces;

public interface IPlaceholderExtractor
{
    Dictionary<string, FieldType> ExtractPlaceholders(Stream docxStream);
}