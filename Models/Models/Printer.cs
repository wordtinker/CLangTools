using LangTools.Shared;
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

        public void LoadStyle()
        {
            // Load CSS file
            string cssContent;
            string cssDir = mediator.Config.StyleDirectoryPath;
            string cssPath = IOTools.CombinePath(cssDir, mediator.currentLanguage.Language);
            cssPath = IOTools.ChangeExtension(cssPath, HTMLPrinter.STYLEEXT);
            if (IOTools.ReadAllText(cssPath, out cssContent))
            {
                printer.LoadCSS(cssContent);
            }
        }

        /// <summary>
        /// Creates marked up output file for a given Item and
        /// return path of the created file.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public string Print(Item root)
        {
            // Get the HTML and save
            string HTML = printer.toHTML(root);
            string outPath = GetOutPath(root.Name);
            return IOTools.SaveFile(outPath, HTML) ? outPath : null; 
        }

        /// <summary>
        /// Provides path to an output file created for a given fileName
        /// of the current project.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetOutPath(string fileName)
        {
            string outName = IOTools.ChangeExtension(fileName, HTMLPrinter.EXT);
            return IOTools.CombinePath(mediator.Config.ProjectOutPath, outName);
        }
    }
}
