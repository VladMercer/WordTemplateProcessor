using System.Diagnostics;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;
using WordTemplateProcessor.web.Interfaces;

namespace WordTemplateProcessor.web.Services;

public class PlaceholderReplacer : IPlaceholderReplacer
{
    private readonly string _wordInserterExePath;
    private readonly string _stampPath;

    public PlaceholderReplacer(IConfiguration config)
    {
        _wordInserterExePath = config["WordImageInserter:ExePathImageInserter"]!;
        _stampPath = config["WordImageInserter:StampPath"]!;
    }

    public async Task<MemoryStream> ReplacePlaceholders(
        Stream docxStream,
        Dictionary<string, string> textFields)
    {
        var intermediate = new MemoryStream();
        docxStream.Position = 0;
        docxStream.CopyTo(intermediate);

        intermediate.Position = 0;
        using (var wordDoc = WordprocessingDocument.Open(intermediate, true))
        {
            var mainPart = wordDoc.MainDocumentPart;
            var xdoc = mainPart.GetXElement();

            foreach (var kv in textFields)
            {
                var placeholder = $"{{{{{kv.Key}}}}}";
                var rx = new Regex(Regex.Escape(placeholder));
                OpenXmlRegex.Replace(
                    xdoc.Descendants(W.p),
                    rx,
                    kv.Value ?? string.Empty,
                    null);
            }

            mainPart.SaveXDocument();
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "wtp");
        Directory.CreateDirectory(tempDir);
        var tempDocxPath = Path.Combine(tempDir, $"{Guid.NewGuid()}.docx");

        await using (var fs = File.Create(tempDocxPath))
        {
            intermediate.Position = 0;
            await intermediate.CopyToAsync(fs);
        }

        var psi = new ProcessStartInfo
        {
            FileName = _wordInserterExePath,
            Arguments = $"\"{tempDocxPath}\" \"{_stampPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (var proc = Process.Start(psi))
        {
            string stdout = await proc.StandardOutput.ReadToEndAsync();
            string stderr = await proc.StandardError.ReadToEndAsync();
            proc.WaitForExit();


            var resultStream = new MemoryStream();
            await using (var fs = File.OpenRead(tempDocxPath))
            {
                await fs.CopyToAsync(resultStream);
            }

            resultStream.Position = 0;

            return resultStream;
        }
    }
}