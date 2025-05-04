namespace WordTemplateProcessor.web.Interfaces;

public interface IPlaceholderReplacer
{
    MemoryStream ReplacePlaceholders(
        Stream docxStream,
        Dictionary<string, string> fields,
        Dictionary<string, byte[]> imageFields);
}