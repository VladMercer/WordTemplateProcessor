using Microsoft.Office.Interop.Word;
using System;
namespace WordToPdfConverter
{
    class WordToPdfConverter
    {
        public static void Convert(string inputPath, string outputPath)
        {
            Application wordApp = new Application();
            Document doc = null;

            try
            {
                wordApp.Visible = false;
                doc = wordApp.Documents.Open(inputPath);
                doc.ExportAsFixedFormat(outputPath, WdExportFormat.wdExportFormatPDF);
            }
            finally
            {
                doc?.Close(false);
                wordApp.Quit();
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: WordToPdfConverter.exe <input.docx> <output.pdf>");
                return;
            }

            Convert(args[0], args[1]);
        }
    }
}
