namespace WordTemplateProcessor.web.Interfaces;

public interface IPdfConverter
{
    Task<byte[]> ConvertToPdfAsync(Stream docxStream);
}