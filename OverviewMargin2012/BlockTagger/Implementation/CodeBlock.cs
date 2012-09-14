// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using System.Globalization;
    using System.Windows;

    public class CodeBlock : IBlockTag
    {
        private SnapshotSpan span;
        private readonly CodeBlock parent;
        private readonly IList<CodeBlock> children = new List<CodeBlock>();
        private readonly string statement;
        private readonly BlockType type;
        private readonly int level;
        private readonly int statementStart;

        public CodeBlock(CodeBlock parent, BlockType type, string statement, SnapshotSpan span, int statementStart, int level)
        {
            this.parent = parent;
            if (parent != null)
            {
                parent.children.Add(this);
            }

            this.statement = statement;
            this.type = type;

            this.span = span;
            this.statementStart = statementStart;
            this.level = level;
        }

        public void SetSpan(SnapshotSpan span)
        {
            this.span = span;
        }

        public CodeBlock Parent
        {
            get { return this.parent; }
        }

        public IList<CodeBlock> Children
        {
            get { return this.children; }
        }

        public SnapshotSpan Span
        {
            get { return this.span; }
        }

        public string Statement
        {
            get { return this.statement; }
        }

        public BlockType Type
        {
            get { return this.type; }
        }

        IBlockTag IBlockTag.Parent
        {
            get
            {
                return this.parent;
            }
        }

        public int Level
        {
            get { return this.level; }
        }

        public SnapshotPoint StatementStart
        {
            get { return new SnapshotPoint(this.Span.Snapshot, this.statementStart); }
        }

        public FrameworkElement Context(BlockColoring coloring)
        {
            return Context(coloring, new Typeface("Lucida Console"), 12.0);
        }

        public FrameworkElement Context(BlockColoring coloring, Typeface typeface, double emSize)
        {
            CodeBlock context = this;
            Stack<CodeBlock> stack = new Stack<CodeBlock>();
            while (true)
            {
                if (context.type == BlockType.Root)
                    break;
                if (context.type != BlockType.Unknown)
                {
                    stack.Push(context);
                }

                context = context.parent;
                if (context.type == BlockType.Namespace)
                    break;
            }

            int indent = 0;
            StringBuilder b = new StringBuilder();
            while (stack.Count != 0)
            {
                context = stack.Pop();
                b.Append(context.statement);

                indent += 2;
                if (stack.Count != 0)
                {
                    b.Append('\r');
                    b.Append(' ', indent);
                }
            }
            return new TextBlob(FormatStatements(b.ToString(), coloring, typeface, emSize));
        }

        private static FormattedText FormatStatements(string tipText, BlockColoring coloring, Typeface typeface, double emSize)
        {
            FormattedText formattedText = new FormattedText(tipText,
                                           CultureInfo.InvariantCulture,
                                           FlowDirection.LeftToRight,
                                           typeface,
                                           emSize,
                                           Brushes.Black);

            if (coloring != null)
            {
                string[] loopKeywords = new string[] { "for", "while", "do", "foreach", "For", "While", "Do", "Loop", "Until", "End While" };
                string[] ifKeywords = new string[] { "if", "else", "switch", "If", "Else", "ElseIf", "End If" };
                string[] methodKeywords = new string[] { "private", "public", "protected", "internal", "sealed", "static",
                                                     "new", "override",
                                                     "int", "double", "void", "bool",
                                                     "Sub", "Function", "Module", "Class", "Property", "Get", "Set",
                                                     "Private", "Public",
                                                     "End Sub", "End Function", "End Module", "End Class", "End Property", "End Get", "End Set"};

                SetColors(coloring.GetBrush(BlockType.Loop), loopKeywords, tipText, formattedText);
                SetColors(coloring.GetBrush(BlockType.Conditional), ifKeywords, tipText, formattedText);
                SetColors(coloring.GetBrush(BlockType.Method), methodKeywords, tipText, formattedText);
            }
            return formattedText;
        }

        private static void SetColors(Brush brush, string[] keywords, string tipText, FormattedText formattedText)
        {
            foreach (string keyword in keywords)
            {
                int index = -1;
                while (true)
                {
                    index = ContainsWord(tipText, keyword, index + 1);
                    if (index == -1)
                        break;

                    formattedText.SetForegroundBrush(brush, index, keyword.Length);
                }
            }
        }

        private static int ContainsWord(string text, string p, int index)
        {
            index = text.IndexOf(p, index);
            if (index == -1)
                return -1;
            else if (((index == 0) || (!char.IsLetterOrDigit(text[index - 1]))) &&
                      ((index + p.Length == text.Length) || (!char.IsLetterOrDigit(text[index + p.Length]))))
            {
                return index;
            }
            else
                return ContainsWord(text, p, index + 1);
        }

        public class TextBlob : FrameworkElement
        {
            private FormattedText text;
            public TextBlob(FormattedText text)
            {
                this.text = text;

                this.Width = text.Width;
                this.Height = text.Height;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                drawingContext.DrawText(this.text, new Point(0.0, 0.0));
            }
        }
    }
}
