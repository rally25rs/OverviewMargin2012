// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.Text;

    class VbParser : IParser
    {
        public VbParser()
        {
        }

        public CodeBlock Parse(ITextSnapshot snapshot, AbortCheck abort)
        {
            CodeBlock root = new CodeBlock(null, BlockType.Root, null, new SnapshotSpan(snapshot, 0, snapshot.Length), 0, 0);
            CodeBlock parent = root;
            Stack<Tuple<int, CodeBlock>> blockOpenings = new Stack<Tuple<int, CodeBlock>>();

            string previousRawText = null;
            string previousFilteredText = null;
            int previousStatementStart = -1;
            int previousLineIndent = 0;

            MacroFilter filter = new MacroFilter(snapshot);
            StringBuilder rawText = new StringBuilder(128);
            StringBuilder filteredText = new StringBuilder(128);
            int statementStart = -1;
            const int tabSize = 4;

            int lineIndent = 0;
            while (filter.Next())
            {
                if (filter.IsAtEol)
                {
                    if (rawText.Length > 0)
                    {
                        previousRawText = rawText.ToString().TrimEnd();
                        previousFilteredText = filteredText.ToString().Trim();
                        previousStatementStart = statementStart;
                        previousLineIndent = lineIndent;
                    }

                    rawText.Clear();
                    filteredText.Clear();
                    statementStart = -1;
                    lineIndent = 0;

                    filter.MoveToEol();
                }
                else
                {
                    char c = filter.Character;

                    if (!filter.InQuote)
                    {
                        filteredText.Append(c);

                        if (statementStart == -1)
                        {
                            if (c == ' ')
                            {
                                ++lineIndent;
                            }
                            else if (c == '\t')
                            {
                                lineIndent = ((lineIndent / tabSize) + 1) * tabSize;
                            }
                            else
                            {
                                statementStart = filter.Position;
                                if (previousFilteredText != null)
                                {
                                    if (lineIndent > previousLineIndent)
                                    {
                                        //Indentation increased, treat it as a start of a block.
                                        BlockType newBlockType = BlockType.Other;
                                        if (StartsWith(previousFilteredText, "For") || StartsWith(previousFilteredText, "Do") || StartsWith(previousFilteredText, "While"))
                                        {
                                            newBlockType = BlockType.Loop;
                                        }
                                        else if (StartsWith(previousFilteredText, "If") || StartsWith(previousFilteredText, "Select") || StartsWith(previousFilteredText, "ElseIf") || StartsWith(previousFilteredText, "Else"))
                                        {
                                            newBlockType = BlockType.Conditional;
                                        }
                                        else if (Contains(previousFilteredText, "Module") || Contains(previousFilteredText, "Class") || Contains(previousFilteredText, "Interface") || Contains(previousFilteredText, "Structure"))
                                        {
                                            newBlockType = BlockType.Class;
                                        }
                                        else if (Contains(previousFilteredText, "Sub") || Contains(previousFilteredText, "Property") || Contains(previousFilteredText, "Function"))
                                        {
                                            newBlockType = BlockType.Method;
                                        }

                                        CodeBlock child = new CodeBlock(parent, newBlockType, previousRawText.ToString().TrimEnd(),
                                                                        new SnapshotSpan(snapshot, previousStatementStart, 0),
                                                                        statementStart, blockOpenings.Count);
                                        blockOpenings.Push(new Tuple<int, CodeBlock>(previousLineIndent, child));
                                        parent = child;
                                    }
                                    else if (lineIndent < previousLineIndent)
                                    {
                                        //Indentation decreased, treat it as the end of a block.
                                        while (blockOpenings.Count > 0)
                                        {
                                            Tuple<int, CodeBlock> block = blockOpenings.Pop();
                                            CodeBlock child = block.Item2;
                                            child.SetSpan(new SnapshotSpan(snapshot, Span.FromBounds(child.Span.Start, statementStart)));

                                            parent = child.Parent;

                                            if (block.Item1 <= lineIndent)
                                                break;      //Things have come back into alignment.
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Do not append two consecutive white space characters (or leading whitespace)
                    if ((!char.IsWhiteSpace(c)) || ((rawText.Length > 0) && (!char.IsWhiteSpace(rawText[rawText.Length - 1]))))
                        rawText.Append(c);

                    if (abort())
                        return null;
                }
            }

            while (blockOpenings.Count > 0)
            {
                CodeBlock child = blockOpenings.Pop().Item2;
                child.SetSpan(new SnapshotSpan(snapshot, Span.FromBounds(child.Span.Start, snapshot.Length)));
            }

            return root;
        }

        private static bool IsSeparator(char c)
        {
            return !(char.IsLetterOrDigit(c) || (c == '_'));
        }

        private static bool Contains(string text, string match)
        {
            int offset = 0;

            while (true)
            {
                int index = text.IndexOf(match, offset);
                if (index == -1)
                    break;

                if (((index == 0) || IsSeparator(text[index - 1])) &&
                    ((index + match.Length == text.Length) || IsSeparator(text[index + match.Length])))
                {
                    return true;
                }

                offset = index + 1;
            }

            return false;
        }

        private static bool StartsWith(string text, string match)
        {
            return (text.StartsWith(match, StringComparison.Ordinal) &&
                    ((text.Length == match.Length) || IsSeparator(text[match.Length])));
        }

        private class QuoteFilter : BaseFilter
        {
            private bool inQuote;
            private bool skipToEol;

            public QuoteFilter(ITextSnapshot snapshot)
                : base(snapshot)
            {
            }

            public bool IsAtEol
            {
                get
                {
                    ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                    return (this.position == line.End);
                }
            }

            public void MoveToEol()
            {
                ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                this.position = Math.Max(line.EndIncludingLineBreak.Position - 1, line.End.Position);
            }

            public bool Next()
            {
                if (this.skipToEol)
                {
                    this.skipToEol = false;
                    ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                    this.position = line.End;
                }
                else
                {
                    if (++(this.position) < this.snapshot.Length)
                    {
                        char opener = base.Character;
                        if (this.inQuote)
                        {
                            if ((opener == '\"') || (this.position >= this.snapshot.GetLineFromPosition(this.position).End))    //Quotes end at the end of the line
                            {
                                this.inQuote = false;
                            }
                        }
                        else
                        {
                            if (opener == '\'')
                            {
                                this.skipToEol = true;
                            }
                            else if ((opener == 'R') && (base.PeekNextChar(1) == 'E') && (base.PeekNextChar(2) == 'M'))
                            {
                                this.skipToEol = true;
                            }
                            else if (opener == '\"')
                            {
                                this.inQuote = true;
                            }
                            else if (opener == '_')
                            {
                                ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                                if (this.position == line.End.Position - 1)
                                {
                                    this.position = line.EndIncludingLineBreak;
                                }
                            }
                        }
                    }
                }

                return (this.position < this.snapshot.Length);
            }

            public bool InQuote { get { return (this.inQuote); } }
        }

        private class MacroFilter : QuoteFilter
        {
            public MacroFilter(ITextSnapshot snapshot)
                : base(snapshot)
            {
            }

            public new bool Next()
            {
                if (base.Next() && !base.InQuote)
                {
                    char opener = base.Character;
                    if (opener == '#')
                    {
                        //In a macro. Continue until we reach the end of line (which could be multiple lines if there is
                        //a continuation character on the macro which could be affected by comments and quoted strings.
                        while (base.Next() && !base.IsAtEol)
                            ;                                   //Intentionally empty.
                    }
                }

                return (this.position < this.snapshot.Length);
            }
        }
    }
}