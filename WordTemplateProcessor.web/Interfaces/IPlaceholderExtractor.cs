using WordTemplateProcessor.web.Enum;

namespace WordTemplateProcessor.web.Interfaces;

public interface IPlaceholderExtractor
{
    Dictionary<string, FieldType> ExtractPlaceholders(Stream docxStream);
}