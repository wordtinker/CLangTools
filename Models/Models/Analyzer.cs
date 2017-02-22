using System.Collections.Generic;
using LangTools.Core;
using LangTools.Shared;

namespace LangTools.Models
{
    /// <summary>
    /// Class that binds file IO and Lexer class.
    /// </summary>
    public class Analyzer
    {
        private IEnumerable<string> dictPathes;
        private Lexer lexer = new Lexer();
        private MainModel mediator;

        public Analyzer(MainModel mediator)
        {
            this.mediator = mediator;
        }

        public void AddDictionaries(IEnumerable<string> dNames)
        {
            this.dictPathes = dNames;
        }

        public void PrepareDictionaries()
        {
            // Load plugin into lexer if we have plugin
            string jsonPluginContent;
            string pluginDir = mediator.Config.StyleDirectoryPath;
            string pluginPath = IOTools.CombinePath(pluginDir, mediator.currentLanguage.Language);
            pluginPath = IOTools.ChangeExtension(pluginPath, Lexer.EXT);
            if (IOTools.ReadAllText(pluginPath, out jsonPluginContent))
            {
                lexer.LoadPlugin(jsonPluginContent);
            }
            // Load dictionaries
            foreach (string path in dictPathes)
            {
                Log.Logger.Debug(string.Format("Analyzing with: {0}", path));
                string content;
                if (IOTools.ReadAllText(path, out content))
                {
                    lexer.LoadDictionary(content);
                }
            }
            // Expand dictionary
            lexer.ExpandDictionary();
        }

        public Document AnalyzeFile(FileStats file)
        {
            Log.Logger.Debug(string.Format("Analyzing the file: {0}", file.FilePath));
            string[] content;
            if (IOTools.ReadAllLines(file.FilePath, out content))
            {
                // Build composite tree
                TokenizerWithStats tknz = new TokenizerWithStats();
                Document root = new Document { Name = file.FileName };
                foreach (string paragraph in content)
                {
                    Item para = new Paragraph();
                    root.AddItem(para);
                    foreach (Token token in tknz.Enumerate(paragraph))
                    {
                        para.AddItem(token);
                    }
                }
                return lexer.AnalyzeText(root);
            }
            else
            {
                return null;
            }
        }
    }
}
