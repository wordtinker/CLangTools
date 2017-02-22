using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace LangTools.Core
{
    /// <summary>
    /// Uses dictionary to mark word tokens as known.
    /// </summary>
    public class Lexer
    {
        public const string EXT = ".json";
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
            foreach(Token token in Tokenizer.Enumerate(content))
            {
                if (token.Type == TokenType.WORD)
                {
                    dict[token.Name] = Source.ORIGINAL; // Will rewrite on duplicate key.
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
        /// Turns the text document into marked document.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Document AnalyzeText(Document root)
        {
            // prevent cyclic reference
            foreach(Token tkn in root.Tokens)
            {
                AnalyzeToken(tkn);
            }

            return root;
        }

        /// <summary>
        /// Does an analysis of single token.
        /// </summary>
        /// <param name="token"></param>
        private void AnalyzeToken(Token token)
        {
            if (token.Type == TokenType.WORD && token.Stats?.Know == Klass.UNDECIDED)
            {
                if (dict.ContainsKey(token.Stats.LWord))
                {
                    if (dict[token.Stats.LWord] == Source.ORIGINAL)
                    {
                        token.Stats.Know = Klass.KNOWN;
                    }
                    else
                    {
                        token.Stats.Know = Klass.MAYBE;
                    }
                }
                else if (IsExpandable(token.Stats.LWord))
                {
                    token.Stats.Know = Klass.MAYBE;
                }
                else
                {
                    token.Stats.Know = Klass.UNKNOWN;
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
    /// Class that yileds word tokens from a string
    /// with bound TokenStats object for each token.
    /// </summary>
    public class TokenizerWithStats
    {
        private TokenStatsFlyweightFactory factory = new TokenStatsFlyweightFactory();
        
        public IEnumerable<Token> Enumerate(string content)
        {
            foreach (Token tkn in Tokenizer.Enumerate(content))
            {
                if (tkn.Type == TokenType.WORD)
                {
                    tkn.Stats = factory.GetTokenStats(tkn.Name);
                }
                yield return tkn;
            }
        }
    }

    /// <summary>
    /// Class that yields word tokens from a string.
    /// </summary>
    public static class Tokenizer
    {
        // Prepare regex statement
        // Any word including words with hebrew specific chars
        // \p{L}+      any character of: UTF macro 'Letter' 1 or more times
        // (
        //   [״'׳"]     \' or ׳(0x5f3) symbol exactly once
        //   \p{L}+      any character of: UTF macro 'Letter' 1 or more times
        // )?       # optionally
        private static Regex rx = new Regex(@"\p{L}+([״'׳""]\p{L}+)?", RegexOptions.Compiled);

        public static IEnumerable<Token> Enumerate(string content)
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
                            Name = content.Substring(position, match.Index - position),
                            Type = TokenType.NONWORD
                        };
                    }
                    // Return the word
                    Token tkn = new Token
                    {
                        Name = match.Value,
                        Type = TokenType.WORD
                    };
                    yield return tkn;
                    // Move position behind the word
                    position = match.Index + match.Length;
                }
                else
                {
                    // No words left, return some trailing characters
                    yield return new Token {Name = content.Substring(position), Type = TokenType.NONWORD };
                    position = content.Length;
                }
            }
        }
    }
}
