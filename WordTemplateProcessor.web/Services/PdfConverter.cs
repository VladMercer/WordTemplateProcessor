using System.Diagnostics;
using WordTemplateProcessor.web.Interfaces;

namespace WordTemplateProcessor.web.Services;

public class PdfConverter : IPdfConverter
{
    private readonly string _wordToPdfExePath;

    public PdfConverter(IConfiguration config)
    {
        _wordToPdfExePath = config["WordImageInserter:ExePathPdfConverter"]!;
    }

    public async Task<byte[]> ConvertToPdfAsync(Stream docxStream)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var docxPath = Path.Combine(tempDir, "template.docx");
        var pdfPath = Path.Combine(tempDir, "template.pdf");

        await using (var fileStream = File.Create(docxPath))
        {
            await docxStream.CopyToAsync(fileStream);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _wordToPdfExePath,
            Arguments = $"\"{docxPath}\" \"{pdfPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new Exception("Не удалось запустить WordToPdfConverter.exe");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || !File.Exists(pdfPath))
        {
            var errorOutput = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Ошибка при конвертации Word → PDF: {errorOutput}");
        }

        return await File.ReadAllBytesAsync(pdfPath);
    }
}