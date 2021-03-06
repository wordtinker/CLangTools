﻿using System.Text;
using System.Linq;

namespace LangTools.Core
{
    /// <summary>
    /// Transfroms word tokens into valid HTML page.
    /// </summary>
    public class HTMLPrinter
    {
        public const string EXT = ".html";
        public const string STYLEEXT = ".css";

        private string css;

        public void LoadCSS(string cssContent)
        {
            css = cssContent;
        }

        public string toHTML(Item root)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'>");
            sb.Append(string.Format("<title>{0}</title></head>", root.Name));
            sb.Append("<body>");
            sb.Append("<article>");
            foreach(Paragraph p in root.Items.OfType<Paragraph>())
            {
                sb.Append("<p>");
                foreach (Token tkn in p.Tokens)
                {
                    if (tkn.Type == TokenType.WORD)
                    {
                        string tag;
                        if (tkn.Stats?.Know == Klass.UNKNOWN)
                        {
                            tag = string.Format(
                                "<span class={0}>{1}</span><sub>{2}</sub>",
                                tkn.Stats.Know,
                                tkn.Name,
                                tkn.Stats.Count);
                        }
                        else
                        {
                            tag = string.Format(
                                "<span class={0}>{1}</span>",
                                tkn.Stats?.Know,
                                tkn.Name);
                        }
                        sb.Append(tag);
                    }
                    else
                    {
                        sb.Append(tkn.Name);
                    }
                }
                sb.Append("</p>");
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
