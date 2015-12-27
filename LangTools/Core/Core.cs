using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace LangTools.Core
{
    internal class Report
    {
        internal readonly List<Token> Tokens;
        internal readonly Dictionary<string, int> UnknownWords;
        internal readonly int Size;
        internal readonly int Known;
        internal readonly int Maybe;

        internal Report(List<Token> tokens,
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

    internal class Analyzer
    {
        private IEnumerable<string> dictPathes;
        private Lexer lexer = new Lexer();
        private string language;

        internal Analyzer(string language)
        {
            this.language = language;
        }

        internal void AddDictionaries(IEnumerable<string> dNames)
        {
            this.dictPathes = dNames;
        }

        internal Report AnalyzeFile(string path)
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

        internal void PrepareDictionaries()
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

        internal Report AnalyzeText(string content)
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
