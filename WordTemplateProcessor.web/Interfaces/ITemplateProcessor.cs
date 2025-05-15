namespace WordTemplateProcessor.web.Interfaces;

public interface IPlaceholderReplacer
{
    Task<MemoryStream> ReplacePlaceholders(
        Stream docxStream,
        Dictionary<string, string> textFields);
}