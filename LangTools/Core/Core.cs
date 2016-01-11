using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace LangTools.Core
{
    /// <summary>
    /// Holds data for a single analyzed file.
    /// </summary>
    class Report
    {
        public readonly List<Token> Tokens;
        public readonly Dictionary<string, int> UnknownWords;
        public readonly int Size;
        public readonly int Known;
        public readonly int Maybe;

        public Report(List<Token> tokens,
            Dictionary<string, int> unknownWords,
            int size, int known, int maybe)
        {
            this.Tokens = tokens;
            this.UnknownWords = unknownWords;
            this.Size = size;
            this.Known = known;
            this.Maybe = maybe;
        }
    }

    /// <summary>
    /// Class that binds file IO and Lexer class.
    /// </summary>
    class Analyzer
    {
        private IEnumerable<string> dictPathes;
        private Lexer lexer = new Lexer();
        private string language;

        public Analyzer(string language)
        {
            this.language = language;
        }

        public void AddDictionaries(IEnumerable<string> dNames)
        {
            this.dictPathes = dNames;
        }

        public Report AnalyzeFile(string path)
        {
            Logger.Write(string.Format("Analyzing the file: {0}", path), Severity.DEBUG);
            string content;
            if (IOTools.ReadAllText(path, out content))
            {
                return lexer.AnalyzeText(content);
            }
            else
            {
                return null;
            }
        }

        public void PrepareDictionaries()
        {
            // Load plugin into lexer if we have plugin
            string jsonPlugin;
            string pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "plugins", language);
            pluginPath = Path.ChangeExtension(pluginPath, ".json");
            if (IOTools.ReadAllText(pluginPath, out jsonPlugin))
            {
                lexer.LoadPlugin(jsonPlugin);
            }
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
            // Expand dictionary
            lexer.ExpandDictionary();
        }
    }

    /// <summary>
    /// Transfroms the text into list of marked tokens.
    /// </summary>
    class Lexer
    {
        /// <summary>
        /// Shows if the word came from dictionary or was expanded by rules.
        /// </summary>
        private enum Source
        {
            ORIGINAL,
            EXPANDED
        }

        /// <summary>
        /// Language rules for extending words. 
        /// </summary>
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

        /// <summary>
        /// Loads json plugin for selected language.
        /// </summary>
        /// <param name="jsonPlugin"></param>
        public void LoadPlugin(string jsonPlugin)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            plug = serializer.Deserialize<Plugin>(jsonPlugin);
        }

        /// <summary>
        /// Loads dictionary. Could be called several times safely.
        /// </summary>
        /// <param name="content"></param>
        public void LoadDictionary(string content)
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

        /// <summary>
        /// Applies the transfromations rules from plugin to
        /// expand the dictionary.
        /// </summary>
        public void ExpandDictionary()
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

        /// <summary>
        /// Turns the text into list of marked tokens and produces stat report.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Report AnalyzeText(string content)
        {
            // Reset text counters
            unknownWords = new Dictionary<string, int>();
            textSizeCount = 0;
            knownWordsCount = 0;
            maybeWordsCount = 0;

            List<Token> tokenList = new List<Token>(new Tokenizer(content));
            tokenList.ForEach(AnalyzeToken);
            
            return new Report(tokenList, unknownWords,
                textSizeCount, knownWordsCount, maybeWordsCount
                );
        }

        /// <summary>
        /// Does an analysis of single token.
        /// </summary>
        /// <param name="token"></param>
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

        /// <summary>
        /// Strips the word of all possible prefixes and
        /// checks if the stem is in the dictionary.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Utility function that safely adds the word
        /// into dictionary.
        /// </summary>
        /// <param name="word"></param>
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
    class Tokenizer : IEnumerable<Token>
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

        public Tokenizer(string content)
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

    /// <summary>
    /// Word token type.
    /// </summary>
    enum TokenType
    {
        WORD,
        NONWORD
    }

    /// <summary>
    /// Word token knowledge type.
    /// </summary>
    enum Klass
    {
        KNOWN,
        MAYBE,
        UNKNOWN
    }

    /// <summary>
    /// Word token.
    /// </summary>
    class Token
    {
        public string Word { get; set; }
        public TokenType Type { get; set; }
        public Klass Know { get; set; }
    }
}
