using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using LangTools.Shared;

namespace LangTools.Core
{
    /// <summary>
    /// Holds data for a single analyzed file.
    /// </summary>
    public class Report
    {
        public List<Token> Tokens { get; internal set; }
        public int Size { get { return Tokens.Distinct().Sum(tkn => tkn.Count); } }
        public int Known { get {
                return Tokens.Distinct().Where(tkn => tkn.Know == Klass.KNOWN).Sum(tkn => tkn.Count); } }
        public int Maybe { get {
                return Tokens.Distinct().Where(tkn => tkn.Know == Klass.MAYBE).Sum(tkn => tkn.Count); } }
        public HashSet<Token> UnknownTokens
        {
            get
            {
                return new HashSet<Token>(Tokens.Where(tkn => tkn.Know == Klass.UNKNOWN));
            }
        }
    }

    /// <summary>
    /// Class that binds file IO and Lexer class.
    /// </summary>
    public class Analyzer
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
            Log.Logger.Debug(string.Format("Analyzing the file: {0}", path));
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
            List<string> layers = plug?.Patterns.Keys.ToList() ?? new List<string>();
            layers.Sort(); // we are sorting strings 
                           // !!! dictionary deeper than "9" will bring errors

            // Create sets to hold the expanding dictionary in the process
            HashSet<string> prevState = new HashSet<string>(dict.Keys);
            HashSet<string> nextState;
            // Transform the word from initial form through layers
            // of transformations.
            foreach (string layer in layers)
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
            List<Token> tokenList = new List<Token>(new Tokenizer(content));
            tokenList.ForEach(AnalyzeToken);

            return new Report
            {
                Tokens = tokenList
            };
        }

        /// <summary>
        /// Does an analysis of single token.
        /// </summary>
        /// <param name="token"></param>
        private void AnalyzeToken(Token token)
        {
            if (token.Type == TokenType.WORD && token.Know == Klass.UNDECIDED)
            {
                if (dict.ContainsKey(token.LWord))
                {
                    if (dict[token.LWord] == Source.ORIGINAL)
                    {
                        token.Know = Klass.KNOWN;
                    }
                    else
                    {
                        token.Know = Klass.MAYBE;
                    }
                }
                else if (IsExpandable(token.LWord))
                {
                    token.Know = Klass.MAYBE;
                }
                else
                {
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
            if (plug != null)
            {
                foreach (string prefix in plug.Prefixes)
                {
                    if (word.StartsWith(prefix, StringComparison.Ordinal) &&
                        dict.Keys.Contains(word.Substring(prefix.Length)))
                    {
                        return true;
                    }
                }
            }
            return false;
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
        private Dictionary<string, Token> uniqueWordTokens = new Dictionary<string, Token>();

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
                    string word = match.Value;
                    string lWord = word.ToLower();
                    Token tkn;
                    if (uniqueWordTokens.ContainsKey(lWord))
                    {
                        tkn = uniqueWordTokens[lWord];
                        tkn.Count += 1;
                    }
                    else
                    {
                        tkn = new Token
                        {
                            Word = word,
                            LWord = lWord,
                            Type = TokenType.WORD,
                            Know = Klass.UNDECIDED,
                            Count = 1
                        };

                        uniqueWordTokens.Add(lWord, tkn);
                    }
                    yield return tkn;
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
    public enum TokenType
    {
        WORD,
        NONWORD
    }

    /// <summary>
    /// Word token knowledge type.
    /// </summary>
    public enum Klass
    {
        UNDECIDED,
        KNOWN,
        MAYBE,
        UNKNOWN
    }

    /// <summary>
    /// Word token.
    /// </summary>
    public class Token
    {
        public string Word { get; set; }
        public string LWord { get; set; } // lower case Word
        public int Count { get; set; } // number of occurences in a text
        public TokenType Type { get; set; }
        public Klass Know { get; set; }
    }
}
