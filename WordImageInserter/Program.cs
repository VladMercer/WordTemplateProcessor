using System;
using System.IO;
using Microsoft.Office.Interop.Word;
using Shape = Microsoft.Office.Interop.Word.Shape;

namespace WordImageInserter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: WordImageInserter.exe <docPath> <imagePath>");
                return;
            }

            string docPath = Path.GetFullPath(args[0]);
            string imagePath = Path.GetFullPath(args[1]);
            Console.WriteLine($"Документ: {docPath}");
            Console.WriteLine($"Изображение: {imagePath}");
            string placeholder = "%stamp%";

            Application wordApp = new Application { Visible = false };
            Document doc = null;

            try
            {
                doc = wordApp.Documents.Open(docPath);

                Range searchRange = doc.Content;
                Find find = searchRange.Find;
                find.Text = placeholder;
                find.Forward = true;
                find.Wrap = WdFindWrap.wdFindStop;

                while (find.Execute())
                {
                    Range found = searchRange.Duplicate;

                    found.Text = "МП";

                    InlineShape inlineShape = found.InlineShapes.AddPicture(
                        FileName: imagePath,
                        LinkToFile: false,
                        SaveWithDocument: true,
                        Range: found);

                    Shape shape = inlineShape.ConvertToShape();
                    shape.LockAspectRatio = Microsoft.Office.Core.MsoTriState.msoTrue;
                    shape.WrapFormat.Type = WdWrapType.wdWrapNone;

                    shape.Height = CmToPoints(3.8);
                    shape.Width = CmToPoints(3.8);

                    shape.RelativeHorizontalPosition =
                        WdRelativeHorizontalPosition.wdRelativeHorizontalPositionCharacter;
                    shape.RelativeVerticalPosition = WdRelativeVerticalPosition.wdRelativeVerticalPositionParagraph;
                    shape.Top = -CmToPoints(1.8);
                    shape.Left = -CmToPoints(1.8);


                    searchRange.Start = found.End;
                    searchRange.End = doc.Content.End;

                    find = searchRange.Find;
                    find.Text = placeholder;
                    find.Forward = true;
                    find.Wrap = WdFindWrap.wdFindStop;
                }

                doc.Save();
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Exception: {0}", ex.Message));
            }
            finally
            {
                if (doc != null) doc.Close(WdSaveOptions.wdSaveChanges);
                wordApp.Quit();
            }
        }

        private static float CmToPoints(double cm)
        {
            return (float)(cm * 28.3464567);
        }
    }
}