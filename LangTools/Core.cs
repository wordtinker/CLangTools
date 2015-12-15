using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

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
        internal readonly List<Token> Tokens;
        internal readonly Dictionary<string, int> UnknownWords;
        internal readonly FileStats Stats;

        internal FileData(List<Token> tokens,
            Dictionary<string, int> unknownWords, FileStats stats)
        {
            this.Tokens = tokens;
            this.UnknownWords = unknownWords;
            this.Stats = stats;
        }
    }

    internal class Analyzer
    {
        private IProgress<RunProgress> progress;
        private IEnumerable<string> filePathes;
        private IEnumerable<string> dictPathes;
        private Lexer lexer;
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
                if (IOTools.ReadAllText(path, out content))
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
            double percentValue = 30;
            double step = 70.0 / filePathes.Count();
            foreach (string path in filePathes)
            {
                Logger.Write(string.Format("Analyzing the file: {0}", path), Severity.DEBUG);
                percentValue += step;
                string content;
                if (IOTools.ReadAllText(path, out content))
                {
                    FileData fData =  lexer.AnalyzeText(path, content);
                    progress.Report(new RunProgress(
                        Convert.ToInt32(percentValue),
                        string.Format("{0} is ready!", Path.GetFileName(path)),
                        fData
                        ));
                }
                else
                {
                    progress.Report(new RunProgress(
                        Convert.ToInt32(percentValue),
                        string.Format("Error in {0}!", Path.GetFileName(path))
                        ));
                }
            }
        }
    }

    

    internal class Lexer
    {
        private enum Source
        {
            ORIGINAL,
            EXPANDED
        }

        private class Plugin
        {
            public Dictionary<string, Dictionary<string, string[]>> Patterns { get; set; }
            public string[] Prefixes { get; set; }
        }

        private Plugin plug;
        // The dictionary to hold known words
        private Dictionary<string, Source> dict = new Dictionary<string, Source>();
        // Current text counters
        private Dictionary<string, int> unknownWords;
        private int textSizeCount;
        private int knownWordsCount;
        private int maybeWordsCount;

        internal void LoadPlugin(string jsonPlugin)
        {
            var serializer = new JavaScriptSerializer();
            plug = serializer.Deserialize<Plugin>(jsonPlugin);
        }

        internal void LoadDictionary(string content)
        {
            content = content.ToLower();
            foreach(Token token in new Tokenizer(content))
            {
                if (token.Type == TokenType.WORD)
                {
                    dict[token.Word] = Source.ORIGINAL; // Will rewrite on duplicate key.
                }
            }
        }

        internal void ExpandDictionary()
        {
            // Sort pattern levels
            List<string> Layers = plug.Patterns.Keys.ToList();
            Layers.Sort(); // we are sorting strings 
                           // !!! dictionary deeper than "9" will bring errors

            // Create sets to hold the expanding dictionary in the process
            HashSet<string> prevState = new HashSet<string>(dict.Keys);
            HashSet<string> nextState;
            // Transform the word from initial form through layers
            // of transformations.
            foreach (string layer in Layers)
            {
                nextState = new HashSet<string>();
                foreach (var pattern in plug.Patterns[layer])
                {
                    Regex rx = new Regex(pattern.Key);
                    foreach (string word in prevState)
                    {
                        // Mutate the word into new form
                        foreach (string replacemnt in pattern.Value)
                        {
                            if (rx.IsMatch(word))
                            {
                                string newWord = rx.Replace(word, replacemnt);
                                nextState.Add(newWord);
                            }
                        }
                    }
                }
                prevState = nextState;
            }

            // Copy the final state into dictionary
            foreach (string word in prevState)
            {
                if (!dict.ContainsKey(word))
                {
                    dict.Add(word, Source.EXPANDED);
                }
            }
        }

        internal FileData AnalyzeText(string source, string content)
        {
            // Reset text counters
            unknownWords = new Dictionary<string, int>();
            textSizeCount = 0;
            knownWordsCount = 0;
            maybeWordsCount = 0;

            List<Token> tokenList = new List<Token>(new Tokenizer(content));
            tokenList.ForEach(AnalyzeToken);

            FileStats stats = new FileStats
            {
                FilePath = source,
                Size = textSizeCount,
                Known = knownWordsCount,
                Maybe = maybeWordsCount,
                Unknown = textSizeCount - knownWordsCount - maybeWordsCount
            };
            return new FileData(tokenList, unknownWords, stats);
        }

        private void AnalyzeToken(Token token)
        {
            if (token.Type == TokenType.WORD)
            {
                string word = token.Word.ToLower();
                textSizeCount += 1;
                if (dict.ContainsKey(word))
                {
                    if (dict[word] == Source.ORIGINAL)
                    {
                        knownWordsCount += 1;
                        token.Know = Klass.KNOWN;
                    }
                    else
                    {
                        maybeWordsCount += 1;
                        token.Know = Klass.MAYBE;
                    }
                }
                else if (IsExpandable(word))
                {
                    maybeWordsCount += 1;
                    token.Know = Klass.MAYBE;
                }
                else
                {
                    AddToUnkownDict(word);
                    token.Know = Klass.UNKNOWN;
                }
            }
        }

        private bool IsExpandable(string word)
        {
            foreach (string prefix in plug.Prefixes)
            {
                if (word.StartsWith(prefix, StringComparison.Ordinal) &&
                    dict.Keys.Contains(word.Substring(prefix.Length)))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddToUnkownDict(string word)
        {
            if (unknownWords.ContainsKey(word))
            {
                unknownWords[word] += 1;
            }
            else
            {
                unknownWords[word] = 1;
            }
        }
    }

    /// <summary>
    /// Takes a string and returns iterator of tokens.
    /// </summary>
    internal class Tokenizer :IEnumerable<Token>
    {
        // Prepare regex statement
        // Any word including words with hebrew specific chars
        // \p{L}+      any character of: UTF macro 'Letter' 1 or more times
        // (
        //   [״'׳"]     \' or ׳(0x5f3) symbol exactly once
        //   \p{L}+      any character of: UTF macro 'Letter' 1 or more times
        // )?       # optionally
        private static Regex rx = new Regex(@"\p{L}+([״'׳""]\p{L}+)?", RegexOptions.Compiled);
        private string content;

        internal Tokenizer(string content)
        {
            this.content = content;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Token> GetEnumerator()
        {
            // Calculate string bounds
            int position = 0;

            while (position < content.Length)
            {
                Match match = rx.Match(content, position);
                if (match.Success)
                {
                    if (position != match.Index) // Some non-letters are left before word
                    {
                        yield return new Token
                        {
                            Word = content.Substring(position, match.Index - position),
                            Type = TokenType.NONWORD
                        };
                    }
                    // Return the word
                    yield return new Token
                    {
                        Word = match.Value,
                        Type = TokenType.WORD
                    };
                    // Move position behind the word
                    position = match.Index + match.Length;
                }
                else
                {
                    // No words left, return some trailing characters
                    yield return new Token {Word = content.Substring(position), Type = TokenType.NONWORD };
                    position = content.Length;
                }
            }
        }
    }

    internal enum TokenType
    {
        WORD,
        NONWORD
    }

    internal enum Klass
    {
        KNOWN,
        MAYBE,
        UNKNOWN
    }

    internal class Token
    {
        public string Word { get; set; }
        public TokenType Type { get; set; }
        public Klass Know { get; set; }
    }
}
