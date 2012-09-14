// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger
{
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    public interface IBlockTag : ITag
    {
        SnapshotSpan Span { get; }
        BlockType Type { get; }
        int Level { get; }
        SnapshotPoint StatementStart { get; }

        IBlockTag Parent { get; }

        FrameworkElement Context(BlockColoring coloring);
        FrameworkElement Context(BlockColoring coloring, Typeface typeface, double emSize);
    }

    public enum BlockType
    {
        Root, Loop, Conditional, Method, Class, Namespace, Other, Unknown
    };
}
