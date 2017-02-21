using LangTools.Shared;
using System.IO;
using LangTools.Core;

namespace LangTools.Models
{
    /// <summary>
    /// Manages the printing of the output page.
    /// </summary>
    internal class Printer
    {
        private MainModel mediator;
        private HTMLPrinter printer = new HTMLPrinter();

        public Printer(MainModel mediator)
        {
            this.mediator = mediator;
        }

        // TODO
        public void LoadStyle()
        {
            // Load CSS file
            //string cssContent;
            //string cssPath = Path.Combine(Directory.GetCurrentDirectory(), "plugins", language);
            //cssPath = Path.ChangeExtension(cssPath, ".css");
            //if (IOTools.ReadAllText(cssPath, out cssContent))
            //{
            //    printer.LoadCSS(cssContent);
            //}
        }

        // TODO desc
        public string Print(Item root)
        {
            // Get the HTML and save
            string HTML = printer.toHTML(root);
            string outPath = GetOutPath(root.Name);
            IOTools.SaveFile(outPath, HTML);
            return outPath;
        }

        // TODO desc
        public string GetOutPath(string fileName)
        {
            // TODO IOTools
            // TODO const
            string outName = Path.ChangeExtension(fileName, ".html");
            return IOTools.CombinePath(mediator.Config.ProjectOutPath, outName);
        }
    }
}
