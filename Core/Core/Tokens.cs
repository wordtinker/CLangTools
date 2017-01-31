using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LangTools.Core
{
    /// <summary>
    /// Abstact composite class that represents a node of document tree.
    /// </summary>
    public abstract class Item
    {
        // List of subnodes.
        protected List<Item> items = new List<Item>();
        /// <summary>
        /// Name of the node
        /// </summary>
        public virtual string Name { get; set; }
        /// <summary>
        /// Number of word tokens in the node and subnodes.
        /// </summary>
        public virtual int Size
        {
            get
            {
                return Tokens.Sum(t => t.Size);
            }
        }
        /// <summary>
        /// Number of known word tokens in the node.
        /// </summary>
        public virtual int Known
        {
            get
            {
                return Tokens.Sum(t => t.Known);
            }
        }
        /// <summary>
        /// Number of words that might be known.
        /// </summary>
        public virtual int Maybe
        {
            get
            {
                return Tokens.Sum(t => t.Maybe);
            }
        }
        /// <summary>
        /// Enumerable of word tokens of the node and subnodes.
        /// </summary>
        public abstract IEnumerable<Token> Tokens { get; }
        /// <summary>
        /// Enumerable of subnodes.
        /// </summary>
        public virtual IEnumerable<Item> Items
        {
            get
            {
                return items;
            }
        }
        /// <summary>
        /// Adds subnode.
        /// </summary>
        /// <param name="item"></param>
        public virtual void AddItem(Item item)
        {
            items.Add(item);
        }
    }

    /// <summary>
    /// Word token node.
    /// </summary>
    public class Token : Item
    {
        public TokenType Type { get; set; }
        public TokenStats Stats { get; set; }

        // Node implementation.
        public override int Size
        {
            get
            {
                return this.Type == TokenType.WORD ? 1 : 0;
            }
        }
        public override int Known
        {
            get
            {
                return this.Stats?.Know == Klass.KNOWN ? 1 : 0;
            }
        }
        public override int Maybe
        {
            get
            {
                return this.Stats?.Know == Klass.MAYBE ? 1 : 0;
            }
        }
        // Token has no subnodes.
        public override IEnumerable<Token> Tokens
        {
            get
            {
                // Empty list
                return new Token[0];
            }
        }
        public override IEnumerable<Item> Items
        {
            get
            {
                // Empty list
                return new Token[0];
            }
        }
        public override void AddItem(Item item) { /* Do nothing */ }
    }

    /// <summary>
    /// Paragraph node.
    /// </summary>
    public class Paragraph : Item
    {
        public override IEnumerable<Token> Tokens
        {
            get
            {
                return this.items.OfType<Token>();
            }
        }
    }
    /// <summary>
    /// Document node.
    /// </summary>
    public class Document : Item
    {
        public override IEnumerable<Token> Tokens
        {
            get
            {
                // Prevent cyclic ref and filter errs. Document should contain only paragraphs.
                foreach (var p in this.items.OfType<Paragraph>())
                {
                    foreach (Token token in p.Tokens)
                    {
                        yield return token;
                    }
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
    /// Object that holds stats for one word.
    /// This object can be shared among several word tokens.
    /// </summary>
    public class TokenStats
    {
        public string LWord { get; set; } // lower case Word
        public int Count { get; set; } // number of occurences in a text
        public Klass Know { get; set; }
    }

    /// <summary>
    /// Flyweight factory that provides shared TokenStats object for a given word.
    /// </summary>
    public class TokenStatsFlyweightFactory
    {
        private Dictionary<string, TokenStats> uniqueWords = new Dictionary<string, TokenStats>();

        public TokenStats GetTokenStats(string word)
        {
            TokenStats tkn;
            string lWord = word.ToLower();
            if (uniqueWords.ContainsKey(lWord))
            {
                tkn = uniqueWords[lWord];
                tkn.Count += 1;
            }
            else
            {
                tkn = new TokenStats
                {
                    LWord = lWord,
                    Know = Klass.UNDECIDED,
                    Count = 1
                };
                uniqueWords.Add(lWord, tkn);
            }
            return tkn;
        } 
    }
}
