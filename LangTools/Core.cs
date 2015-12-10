using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LangTools
{
    internal class RunProgress
    {
        internal readonly int Percent;
        internal readonly string Message;
        internal readonly FileData Data;

        internal RunProgress(int progressValue, string message = "", FileData data = null)
        {
            this.Percent = progressValue;
            this.Message = message;
            this.Data = data;
        }
    }


    internal class FileData
    {
        internal readonly string Path;
        internal readonly int Size;
        internal readonly int Known;
        internal readonly int Maybe;
        internal readonly int Unknown;

        internal FileData(string path, int size, int known, int maybe, int unknown)
        {
            this.Path = path;
            this.Size = size;
            this.Known = known;
            this.Maybe = maybe;
            this.Unknown = unknown;
        }
    }

    internal class Analyzer
    {
        internal IProgress<RunProgress> progress;
        IEnumerable<string> filePathes;
        IEnumerable<string> dictPathes;
        Lexer lexer;
        private string language;

        internal Analyzer(string language)
        {
            this.language = language;
        }

        internal void AddFiles(IEnumerable<string> fNames)
        {
            this.filePathes = fNames;
        }

        internal void AddDictionaries(IEnumerable<string> dNames)
        {
            this.dictPathes = dNames;
        }

        internal void Run(IProgress<RunProgress> progress)
        {
            this.progress = progress;

            progress.Report(new RunProgress(0));
            PrepareDictionaries();
            StartAnalysis();
        }

        private void PrepareDictionaries()
        {
            //Build lexer for current project
            lexer = new Lexer();
            // Load plugin into lexer if we have plugin
            string jsonPlugin;
            string pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "plugins", language);
            pluginPath = Path.ChangeExtension(pluginPath, ".json");
            if (IOTools.ReadAllText(pluginPath, out jsonPlugin))
            {
                lexer.LoadPlugin(jsonPlugin);
            }
            progress.Report(new RunProgress(10));
            // Load dictionaries
            foreach (string path in dictPathes)
            {
                Logger.Write(string.Format("Analyzing with: {0}", path), Severity.DEBUG);
                string content;
                if(IOTools.ReadAllText(path, out content))
                {
                    lexer.LoadDictionary(content);
                }
            }
            progress.Report(new RunProgress(20));
            // Expand dictionary
            lexer.ExpandDictionary();
            progress.Report(new RunProgress(30, "Dictionaries are ready."));
        }

        private void StartAnalysis()
        {
            int percentValue = 30;
            int step = 70 / filePathes.Count();
            foreach (string path in filePathes)
            {
                Logger.Write(string.Format("Analyzing the file: {0}", path), Severity.DEBUG);
                percentValue += step;
                string content;
                if(IOTools.ReadAllText(path, out content))
                {
                    FileData fData =  lexer.AnalyzeText(path, content);
                    progress.Report(new RunProgress(
                        percentValue,
                        string.Format("{0} ready!", Path.GetFileName(path)),
                        fData
                        ));
                }
                else
                {
                    progress.Report(new RunProgress(
                        percentValue,
                        string.Format("Error in {0}!", Path.GetFileName(path))
                        ));
                }
            }
        }
    }

    internal class Lexer
    {
        // TODO
        internal void LoadPlugin(string jsonPlugin)
        {
            // TODO
        }
        internal void LoadDictionary(string content)
        {
            // TODO
        }
        internal void ExpandDictionary()
        {
            // TODO
        }

        internal FileData AnalyzeText(string source, string content)
        {
            // TODO //lexifiedText = ??? 
            // TODO //unknownDict = Dictionary
            int textSize = 0;
            int known = 0;
            int maybe = 0;
            int unknown = 0;

            // TODO: stub
            System.Threading.Thread.Sleep(1000);
            // TODO: stub
            return new FileData(source, 4800, 4000, 500, 300);
        }
    }
}
