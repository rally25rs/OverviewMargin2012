// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.StructureAdornment
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Extensions.BlockTagger;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Tagging;
    using System.ComponentModel.Composition;

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class ClassColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Black; } }
        public override EditorOptionKey<Color> Key { get { return StructureAdornmentManager.ClassColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class ConditionalColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Green; } }
        public override EditorOptionKey<Color> Key { get { return StructureAdornmentManager.ConditionalColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class LoopColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Red; } }
        public override EditorOptionKey<Color> Key { get { return StructureAdornmentManager.LoopColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class MethodColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Blue; } }
        public override EditorOptionKey<Color> Key { get { return StructureAdornmentManager.MethodColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class UnknownColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Gray; } }
        public override EditorOptionKey<Color> Key { get { return StructureAdornmentManager.UnknownColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class LineWidth : ViewOptionDefinition<double>
    {
        public override double Default { get { return 1.0; } }
        public override EditorOptionKey<double> Key { get { return StructureAdornmentManager.LineWidthId; } }
    }

    class StructureAdornmentManager : MouseProcessorBase
    {
        public static readonly EditorOptionKey<bool> EnabledOption = new EditorOptionKey<bool>("StructureAdornment/Enabled");

        public static readonly EditorOptionKey<Color> ClassColorId = new EditorOptionKey<Color>("StructureAdornment/ClassColor");
        public static readonly EditorOptionKey<Color> ConditionalColorId = new EditorOptionKey<Color>("StructureAdornment/ConditionalColor");
        public static readonly EditorOptionKey<Color> LoopColorId = new EditorOptionKey<Color>("StructureAdornment/LoopColor");
        public static readonly EditorOptionKey<Color> MethodColorId = new EditorOptionKey<Color>("StructureAdornment/MethodColor");
        public static readonly EditorOptionKey<Color> UnknownColorId = new EditorOptionKey<Color>("StructureAdornment/UnknownColor");
        public static readonly EditorOptionKey<double> LineWidthId = new EditorOptionKey<double>("StructureAdornment/LineWidth");

        private IWpfTextView view;
        private IAdornmentLayer layer;
        private ITagAggregator<IBlockTag> blockTagger;
        private BlockColoring blockColoring;
        private HashSet<VisibleBlock> visibleBlocks = new HashSet<VisibleBlock>();
        private bool showAdornments;
        private ToolTip tipWindow;

        public static StructureAdornmentManager Create(IWpfTextView view, StructureAdornmentFactory factory)
        {
            return view.Properties.GetOrCreateSingletonProperty<StructureAdornmentManager>(delegate { return new StructureAdornmentManager(view, factory); });
        }

        private StructureAdornmentManager(IWpfTextView view, StructureAdornmentFactory factory)
        {
            this.view = view;
            this.layer = view.GetAdornmentLayer("StructureAdornmentLayer");

            factory.LoadOption(view.Options, StructureAdornmentManager.ClassColorId.Name);
            factory.LoadOption(view.Options, StructureAdornmentManager.ConditionalColorId.Name);
            factory.LoadOption(view.Options, StructureAdornmentManager.LoopColorId.Name);
            factory.LoadOption(view.Options, StructureAdornmentManager.MethodColorId.Name);
            factory.LoadOption(view.Options, StructureAdornmentManager.UnknownColorId.Name);
            factory.LoadOption(view.Options, StructureAdornmentManager.EnabledOption.Name);
            factory.LoadOption(view.Options, StructureAdornmentManager.LineWidthId.Name);

            this.blockColoring = new BlockColoring(view.Options.GetOptionValue(StructureAdornmentManager.LineWidthId),
                                                   view.Options.GetOptionValue(StructureAdornmentManager.ClassColorId),
                                                   view.Options.GetOptionValue(StructureAdornmentManager.ConditionalColorId),
                                                   view.Options.GetOptionValue(StructureAdornmentManager.LoopColorId),
                                                   view.Options.GetOptionValue(StructureAdornmentManager.MethodColorId),
                                                   view.Options.GetOptionValue(StructureAdornmentManager.UnknownColorId));

            this.blockTagger = factory.TagAggregatorFactoryService.CreateTagAggregator<IBlockTag>(view);

            view.Closed += OnClosed;

            view.Options.OptionChanged += this.OnOptionsChanged;
        }

        private void OnOptionsChanged(object sender, EventArgs e)
        {
            bool newValue = this.view.Options.GetOptionValue<bool>(StructureAdornmentManager.EnabledOption);
            if (this.showAdornments != newValue)
            {
                this.showAdornments = newValue;

                if (newValue)
                {
                    this.view.LayoutChanged += OnLayoutChanged;
                    this.blockTagger.BatchedTagsChanged += OnTagsChanged;

                    this.RedrawAllAdornments();
                }
                else
                {
                    this.view.LayoutChanged -= OnLayoutChanged;
                    this.blockTagger.BatchedTagsChanged -= OnTagsChanged;

                    this.visibleBlocks.Clear();
                    this.layer.RemoveAllAdornments();
                }
            }
        }

        void OnClosed(object sender, EventArgs e)
        {
            if (this.showAdornments)
            {
                this.view.LayoutChanged -= OnLayoutChanged;
                this.blockTagger.BatchedTagsChanged -= OnTagsChanged;
            }
            view.Options.OptionChanged -= this.OnOptionsChanged;
            view.Closed -= OnClosed;

            //No need to dispose of blockTagger: aggregators on the view are automatically closed when the view is closed.
        }

        void OnTagsChanged(object sender, EventArgs e)
        {
            this.RedrawAllAdornments();
        }

        void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            this.RedrawAdornments(e.NewOrReformattedSpans);
        }

        void RedrawAllAdornments()
        {
            //Remove the existing adornments.
            this.visibleBlocks.Clear();
            this.layer.RemoveAllAdornments();

            this.RedrawAdornments(new NormalizedSnapshotSpanCollection(new SnapshotSpan(this.view.TextSnapshot, 0, this.view.TextSnapshot.Length)));
        }

        void RedrawAdornments(NormalizedSnapshotSpanCollection newOrReformattedSpans)
        {
            if (this.showAdornments && (this.view.TextViewLines != null) && !this.view.IsClosed)
            {
                //Recreate the adornments for the visible text.
                var tags = this.blockTagger.GetTags(this.view.TextViewLines.FormattedSpan);
                foreach (var tag in tags)
                {
                    this.CreateBlockAdornments(tag, newOrReformattedSpans);
                }
            }
        }

        private void CreateBlockAdornments(IMappingTagSpan<IBlockTag> tag, NormalizedSnapshotSpanCollection newOrReformattedSpans)
        {
            NormalizedSnapshotSpanCollection spans = tag.Span.GetSpans(this.view.TextSnapshot);
            if (spans.Count > 0)
            {
                //Get the start of the tag's span (which could be out of the view or not even mappable to
                //the view's text snapshot).
                var start = this.view.BufferGraph.MapUpToSnapshot(tag.Tag.Span.Start, PointTrackingMode.Positive, PositionAffinity.Predecessor, this.view.TextSnapshot);
                if (start.HasValue)
                {
                    double x = -1.0;
                    foreach (var span in spans)
                    {
                        if (newOrReformattedSpans.IntersectsWith(new NormalizedSnapshotSpanCollection(span)))
                        {
                            ITextViewLine spanTop = this.view.TextViewLines.GetTextViewLineContainingBufferPosition(span.Start);
                            double yTop = (spanTop == null) ? this.view.ViewportTop : spanTop.Bottom;

                            ITextViewLine spanBottom = this.view.TextViewLines.GetTextViewLineContainingBufferPosition(span.End);
                            double yBottom = (spanBottom == null) ? this.view.ViewportBottom : spanBottom.Top;

                            if (yBottom > yTop)
                            {
                                if (x < 0.0)
                                {
                                    //Get the x-coordinate of the adornment == middle of the character that starts the block.
                                    //The first non-whitespace character on the line that defines the start of the block.
                                    ITextSnapshotLine startLine = start.Value.GetContainingLine();
                                    int i = startLine.Start;
                                    while (i < startLine.End)
                                    {
                                        char c = this.view.TextSnapshot[i];
                                        if ((c != ' ') && (c != '\t'))
                                            break;
                                        ++i;
                                    }
                                    SnapshotPoint blockStart = new SnapshotPoint(this.view.TextSnapshot, i);
                                    ITextViewLine tagTop = this.view.GetTextViewLineContainingBufferPosition(blockStart);
                                    TextBounds bounds = tagTop.GetCharacterBounds(blockStart);
                                    x = Math.Floor((bounds.Left + bounds.Right) * 0.5);   //Make sure this is only a pixel wide.
                                }

                                this.CreateBlockAdornment(tag.Tag, span, x, yTop, yBottom);
                            }
                        }
                    }
                }
            }
        }

        private void CreateBlockAdornment(IBlockTag tag, SnapshotSpan span, double x, double yTop, double yBottom)
        {
            LineGeometry line = new LineGeometry(new Point(x, yTop), new Point(x, yBottom));

            GeometryDrawing drawing = new GeometryDrawing(null, this.blockColoring.GetPen(tag), line);
            drawing.Freeze();

            DrawingImage drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();

            Image image = new Image();
            image.Source = drawingImage;

            VisibleBlock block = new VisibleBlock(tag, x, yTop, yBottom);

            Canvas.SetLeft(image, x);
            Canvas.SetTop(image, yTop);
            this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, block, image, OnAdornmentRemoved);

            this.visibleBlocks.Add(block);
        }

        private void OnAdornmentRemoved(object tag, UIElement element)
        {
            this.visibleBlocks.Remove((VisibleBlock)tag);
        }

        private struct VisibleBlock
        {
            public readonly IBlockTag tag;
            public readonly double x;
            public readonly double yTop;
            public readonly double yBottom;

            public VisibleBlock(IBlockTag tag, double x, double yTop, double yBottom)
            {
                this.tag = tag;
                this.x = x;
                this.yTop = yTop;
                this.yBottom = yBottom;
            }
        }


        public override void PreprocessMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            ITextViewLineCollection textLines = this.view.TextViewLines;
            if ((textLines != null) && (this.visibleBlocks.Count > 0))
            {
                ITextViewLine firstVisible = textLines.FirstVisibleLine;

                int screenTop = (firstVisible.VisibilityState == VisibilityState.FullyVisible)
                                ? firstVisible.Start
                                : firstVisible.EndIncludingLineBreak;
                Point pt = e.GetPosition(this.view.VisualElement);
                pt.X += this.view.ViewportLeft;
                pt.Y += this.view.ViewportTop;

                foreach (VisibleBlock block in this.visibleBlocks)
                {
                    if ((Math.Abs(pt.X - block.x) < 4.0) &&
                        (pt.Y >= block.yTop) && (pt.Y <= block.yBottom))
                    {
                        SnapshotPoint? statementStart = this.view.BufferGraph.MapUpToSnapshot(block.tag.StatementStart, PointTrackingMode.Positive, PositionAffinity.Successor, this.view.TextSnapshot);
                        if (statementStart.HasValue && (statementStart.Value < screenTop))
                        {
                            if (this.tipWindow == null)
                            {
                                this.tipWindow = new ToolTip();

                                this.tipWindow.ClipToBounds = true;

                                this.tipWindow.Placement = PlacementMode.Top;
                                this.tipWindow.PlacementTarget = this.view.VisualElement;
                                this.tipWindow.HorizontalAlignment = HorizontalAlignment.Left;
                                this.tipWindow.HorizontalContentAlignment = HorizontalAlignment.Left;

                                this.tipWindow.VerticalAlignment = VerticalAlignment.Top;
                                this.tipWindow.VerticalContentAlignment = VerticalAlignment.Top;
                            }

                            this.tipWindow.PlacementRectangle = new Rect(block.x, 0.0, 0.0, 0.0);

                            this.tipWindow.IsOpen = true;
                            FrameworkElement context = block.tag.Context(this.blockColoring,
                                                                         this.view.FormattedLineSource.DefaultTextProperties.Typeface,
                                                                         this.view.FormattedLineSource.DefaultTextProperties.FontRenderingEmSize);

                            //The width of the view is in zoomed coordinates so factor the zoom factor into the tip window width computation.
                            double zoom = this.view.ZoomLevel / 100.0;
                            this.tipWindow.MaxWidth = Math.Max(100.0, this.view.ViewportWidth * zoom * 0.5);
                            this.tipWindow.MinHeight = this.tipWindow.MaxHeight = context.Height + 12.0;

                            this.tipWindow.Content = context;

                            return;
                        }
                    }
                }
            }

            if (this.tipWindow != null)
            {
                this.tipWindow.IsOpen = false;
                this.tipWindow = null;
            }
        }

        public override void PreprocessMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            if (this.tipWindow != null)
            {
                this.tipWindow.IsOpen = false;
                this.tipWindow = null;
            }
        }
    }
}