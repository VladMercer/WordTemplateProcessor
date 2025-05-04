using System.Diagnostics;
using WordTemplateProcessor.web.Interfaces;

namespace WordTemplateProcessor.web.Services;

public class PdfConverter : IPdfConverter
{
    public async Task<byte[]> ConvertToPdfAsync(Stream docxStream)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var docxPath = Path.Combine(tempDir, "template.docx");
        var pdfPath = Path.Combine(tempDir, "template.pdf");

        await using (var fileStream = File.Create(docxPath))
            await docxStream.CopyToAsync(fileStream);

        var startInfo = new ProcessStartInfo
        {
            FileName = "soffice", 
            Arguments = $"--headless --convert-to pdf \"{docxPath}\" --outdir \"{tempDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        await process.WaitForExitAsync();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath);

        Directory.Delete(tempDir, true); 

        return pdfBytes;
    }
}