// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.Text;

    abstract class  BraceParser : IParser
    {
        public BraceParser()
        {
        }

        protected abstract BlockType FindType(CodeBlock parent, string statement);

        public CodeBlock Parse(ITextSnapshot snapshot, AbortCheck abort)
        {
            CodeBlock root = new CodeBlock(null, BlockType.Root, null, new SnapshotSpan(snapshot, 0, snapshot.Length), 0, 0);

            CodeBlock parent = root;

            Stack<CodeBlock> blockOpenings = new Stack<CodeBlock>();

            bool leadingWhitespace = true;
            int statementStart = -1;
            StringBuilder currentStatement = new StringBuilder();

            SnapshotFilter filter = new SnapshotFilter(snapshot);
            while (filter.Next())
            {
                int position = filter.Position;
                char c = filter.Character;

                if (statementStart == -1)
                    statementStart = position;
                else if (leadingWhitespace)
                {
                    leadingWhitespace = char.IsWhiteSpace(c);
                    statementStart = position;
                }

                if (!filter.InQuote)
                {
                    if (c == '{')
                    {
                        CodeBlock child = CreateCodeBlock(parent, currentStatement, new SnapshotSpan(snapshot, position, 0), statementStart, blockOpenings.Count + 1);

                        blockOpenings.Push(child);

                        parent = child;
                    }
                    else if (c == '}')
                    {
                        if (blockOpenings.Count > 0)
                        {
                            CodeBlock child = blockOpenings.Pop();
                            child.SetSpan(new SnapshotSpan(snapshot, Span.FromBounds(child.Span.Start, position + 1)));

                            parent = child.Parent;
                        }
                    }
                }

                if (filter.EOS)
                {
                    currentStatement.Remove(0, currentStatement.Length);
                    statementStart = -1;
                    leadingWhitespace = true;
                }
                else
                    currentStatement.Append(c);

                if (abort())
                    return null;
            }

            while (blockOpenings.Count > 0)
            {
                CodeBlock child = blockOpenings.Pop();
                child.SetSpan(new SnapshotSpan(snapshot, Span.FromBounds(child.Span.Start, snapshot.Length)));
            }

            return root;
        }

        private CodeBlock CreateCodeBlock(CodeBlock parent, StringBuilder rawStatement, SnapshotSpan span, int statementStart, int level)
        {
            string statement = ExtractStatement(rawStatement);
            BlockType type = this.FindType(parent, statement);
            CodeBlock child = new CodeBlock(parent, type, statement,
                                            span, statementStart, level);

            return child;
        }

        private class SnapshotFilter : QuoteFilter
        {
            private bool eos = false;
            private int braceDepth = 0;
            private Stack<int> nestedBraceDepth = new Stack<int>();

            public SnapshotFilter(ITextSnapshot snapshot)
                : base(snapshot)
            {
            }

            public new bool Next()
            {
                if (!base.Next())
                    return false;

                this.eos = false;
                if (!base.InQuote)
                {
                    char c = base.Character;

                    if (c == ';')
                    {
                        //Whether or not a ; counts as an end of statement depends on context.
                        //      foo();                          <--This does
                        //      for (int i = 0; (i < 10); ++i)  <-- These don't
                        //          bar(delegate{
                        //                  baz();              <-- this does
                        //
                        // Basically, it is an end of statement unless it is contained in an open parenthesis and an open brace
                        // hasn't been encountered since the open paranthesis.
                        this.eos = (this.nestedBraceDepth.Count == 0) || (this.nestedBraceDepth.Peek() < this.braceDepth);
                    }
                    else if (c == '(')
                    {
                        this.nestedBraceDepth.Push(this.braceDepth);
                    }
                    else if (c == ')')
                    {
                        if (this.nestedBraceDepth.Count > 0)
                            this.nestedBraceDepth.Pop();
                    }
                    else if (c == '{')
                    {
                        ++(this.braceDepth);
                        this.eos = true;
                    }
                    else if (c == '}')
                    {
                        --(this.braceDepth);
                        this.eos = true;
                    }
                }

                return true;
            }

            public bool EOS { get { return this.eos; } }
        }

        private class QuoteFilter : BaseFilter
        {
            private char quote = ' ';
            private bool escape = false;

            public QuoteFilter(ITextSnapshot snapshot)
                : base(snapshot)
            {
            }

            public bool Next()
            {
                if (++(this.position) < this.snapshot.Length)
                {
                    bool wasEscaped = this.escape;
                    this.escape = false;

                    char opener = base.Character;
                    if (this.quote == ' ')
                    {
                        if (opener == '#')
                        {
                            ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                            this.position = line.End;
                        }
                        else if ((opener == '\'') || (opener == '\"'))
                            this.quote = opener;
                        else if (opener == '@')
                        {
                            char next = this.PeekNextChar();
                            if (next == '\"')
                            {
                                this.quote = '@';
                                this.position += 1;
                            }
                        }
                        else if (opener == '/')
                        {
                            char next = this.PeekNextChar();
                            if (next == '/')
                            {
                                ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                                this.position = line.End;
                            }
                            else if (next == '*')
                            {
                                this.position += 2;

                                while (this.position < this.snapshot.Length)
                                {
                                    if ((this.snapshot[this.position] == '*') && (this.PeekNextChar() == '/'))
                                    {
                                        this.position += 2;
                                        break;
                                    }

                                    ++(this.position);
                                }
                            }
                        }
                    }
                    else if ((this.quote != '@') && (opener == '\\') && !wasEscaped)
                    {
                        this.escape = true;
                    }
                    else if (((opener == this.quote) || ((opener == '\"') && (this.quote == '@'))) && !wasEscaped)
                    {
                        this.quote = ' ';
                    }
                    else if ((this.quote == '\"') || (this.quote == '\''))
                    {
                        ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                        if (line.End == this.position)
                        {
                            //End simple quotes at the end of the line.
                            this.quote = ' ';
                        }
                    }

                    return (this.position < this.snapshot.Length);
                }

                return false;
            }

            public bool InQuote { get { return (this.quote != ' '); } }
        }

        private static string ExtractStatement(StringBuilder statement)
        {
            bool eatWhiteSpace = true;
            StringBuilder compressedStatement = new StringBuilder(statement.Length);

            foreach (char c in statement.ToString())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!eatWhiteSpace)
                    {
                        eatWhiteSpace = true;
                        compressedStatement.Append(' ');
                    }
                }
                else
                {
                    eatWhiteSpace = false;
                    compressedStatement.Append(c);
                }
            }
            return compressedStatement.ToString();
        }

        public static bool ContainsWord(string text, string p)
        {
            int index = text.IndexOf(p, StringComparison.Ordinal);
            return (index >= 0) &&
                   ((index == 0) || (!char.IsLetterOrDigit(text[index - 1]))) &&
                   ((index + p.Length == text.Length) || (!char.IsLetterOrDigit(text[index + p.Length])));
        }
    }
}
