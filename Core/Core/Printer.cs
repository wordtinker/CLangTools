using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LangTools.Shared;

namespace LangTools.Core
{
    /// <summary>
    /// Manages the printing of the output page.
    /// </summary>
    public class Printer
    {
        private HTMLPrinter printer = new HTMLPrinter();

        public Printer(string language)
        {
            // Load CSS file
            string cssContent;
            string cssPath = Path.Combine(Directory.GetCurrentDirectory(), "plugins", language);
            cssPath = Path.ChangeExtension(cssPath, ".css");
            if (IOTools.ReadAllText(cssPath, out cssContent))
            {
                printer.LoadCSS(cssContent);
            }
        }

        public void Print(string fileName, string project, string language, List<Token> tokens)
        {
            // Create proper name for output file;
            string outName = Path.ChangeExtension(fileName, ".html");
            // TODO
            // string outPath = Path.Combine(language, (string)App.Current.Properties["outputDir"],
            //                              project, outName);
            string outPath = Path.Combine(language, "output",
                                          project, outName);
            // Get the HTML and save
            string HTML = printer.toHTML(fileName, tokens);
            IOTools.SaveFile(outPath, HTML);
        }
    }

    /// <summary>
    /// Transfroms word tokens into valid HTML page.
    /// </summary>
    class HTMLPrinter
    {
        private class Wrapper : IEnumerable<string>
        {
            private List<Token> tokens;
            private List<string> tags = new List<string>();

            public Wrapper(List<Token> tokens)
            {
                this.tokens = tokens;
            }

            private string getParagraph(string endOfParagraph)
            {
                tags.Add(endOfParagraph);
                tags.Insert(0, "<p>");
                tags.Add("</p>");
                string result = string.Join("", tags);
                tags.Clear();
                return result;
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach (Token tkn in tokens)
                {
                    if (tkn.Type == TokenType.WORD)
                    {
                        // Put into list of words
                        string tag = string.Format(
                            "<span class={0}>{1}</span>",
                            tkn.Know,
                            tkn.Word);
                        tags.Add(tag);
                    }
                    else if (tkn.Word.Contains("\n"))
                    {
                        // yield the paragraphs
                        foreach (string p in tkn.Word.Split('\n'))
                        {
                            yield return getParagraph(p);
                        }
                    }
                    else
                    {
                        // Put !?,. etc into the list
                        tags.Add(tkn.Word);
                    }
                }
                // if list is not empty yield the last paragraph
                yield return getParagraph("");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private string css;

        public void LoadCSS(string cssContent)
        {
            css = cssContent;
        }

        public string toHTML(string fileName, List<Token> tokens)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'>");
            sb.Append(string.Format("<title>{0}</title></head>", fileName));
            sb.Append("<body>");
            sb.Append("<article>");
            foreach (string paragraph in new Wrapper(tokens))
            {
                sb.Append(paragraph);
            }
            sb.Append("</article>");
            sb.Append("<style>");
            if (css != null)
            {
                sb.Append(css);
            }
            else
            {
                // Use default style
                sb.Append(@"
    body {font-family:sans-serif;
            line-height: 1.5;}
    span.KNOWN
            {background-color: white;
            font-weight: normal;font-style: normal;
            border-bottom: 3px solid green;}
    span.MAYBE
            {background-color: white;
            font-weight: normal;font-style: normal;
            border-bottom: 3px solid yellowgreen;}
                ");
            }
            sb.Append("</style></body></html>");
            return sb.ToString();
        }
    }
}
