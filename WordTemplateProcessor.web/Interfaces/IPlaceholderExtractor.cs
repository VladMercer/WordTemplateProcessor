namespace WordTemplateProcessor.web.Interfaces;

public interface IPlaceholderExtractor
{
    IList<string> ExtractPlaceholders(Stream docxStream);
}