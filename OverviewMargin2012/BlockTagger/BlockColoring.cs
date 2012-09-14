// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger
{
    using System.ComponentModel.Composition;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text.Editor;

    public class BlockColoring
    {
        struct Coloring
        {
            public readonly Color Color;
            public readonly Pen Pen;
            public readonly Brush Brush;

            public Coloring(Color color, double width)
            {
                this.Color = color;
                this.Brush = new SolidColorBrush(color);
                this.Brush.Freeze();

                this.Pen = new Pen(this.Brush, width);
                this.Pen.Freeze();
            }
        }

        private readonly Coloring classColoring;
        private readonly Coloring conditionalColoring;
        private readonly Coloring loopColoring;
        private readonly Coloring methodColoring;
        private readonly Coloring unknownColoring;

        public BlockColoring(double width, Color classColor, Color conditionalColor, Color loopColor, Color methodColor, Color unknownColor)
        {
            this.classColoring = new Coloring(classColor, width);
            this.conditionalColoring = new Coloring(conditionalColor, width);
            this.loopColoring = new Coloring(loopColor, width);
            this.methodColoring = new Coloring(methodColor, width);
            this.unknownColoring = new Coloring(unknownColor, width);
        }

        public Pen GetPen(IBlockTag tag)
        {
            return this.GetColoring(tag.Type).Pen;
        }
        public Pen GetPen(BlockType type)
        {
            return this.GetColoring(type).Pen;
        }

        public Brush GetBrush(IBlockTag tag)
        {
            return this.GetColoring(tag.Type).Brush;
        }
        public Brush GetBrush(BlockType type)
        {
            return this.GetColoring(type).Brush;
        }

        public Color GetColor(IBlockTag tag)
        {
            return this.GetColoring(tag.Type).Color;
        }
        public Color GetColor(BlockType type)
        {
            return this.GetColoring(type).Color;
        }

        private Coloring GetColoring(BlockType type)
        {
            switch (type)
            {
                case BlockType.Loop:
                    return this.loopColoring;
                case BlockType.Conditional:
                    return this.conditionalColoring;
                case BlockType.Class:
                    return this.classColoring;
                case BlockType.Method:
                    return this.methodColoring;
                default:
                    return this.unknownColoring;
            }
        }
    }
}
