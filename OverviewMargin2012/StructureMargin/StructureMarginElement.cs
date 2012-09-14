// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.StructureMargin
{
    using System;
    using System.ComponentModel.Composition;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using BlockTagger;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Extensions.OverviewMargin;

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class ClassColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Black; } }
        public override EditorOptionKey<Color> Key { get { return StructureMarginElement.ClassColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class ConditionalColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Green; } }
        public override EditorOptionKey<Color> Key { get { return StructureMarginElement.ConditionalColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class LoopColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Red; } }
        public override EditorOptionKey<Color> Key { get { return StructureMarginElement.LoopColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class MethodColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Blue; } }
        public override EditorOptionKey<Color> Key { get { return StructureMarginElement.MethodColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class UnknownColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Gray; } }
        public override EditorOptionKey<Color> Key { get { return StructureMarginElement.UnknownColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class LineWidth : ViewOptionDefinition<double>
    {
        public override double Default { get { return 2.0; } }
        public override EditorOptionKey<double> Key { get { return StructureMarginElement.LineWidthId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class GapWidth : ViewOptionDefinition<double>
    {
        public override double Default { get { return 1.0; } }
        public override EditorOptionKey<double> Key { get { return StructureMarginElement.GapWidthId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class StructureMarginEnabled : ViewOptionDefinition<bool>
    {
        public override bool Default { get { return true; } }
        public override EditorOptionKey<bool> Key { get { return StructureMarginElement.EnabledOptionId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    public sealed class MethodEllipseColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Color.FromArgb(0x015, 0x00, 0x00, 0xff); } }

        public override EditorOptionKey<Color> Key { get { return StructureMarginElement.MethodEllipseColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    public sealed class StructureMarginWidth : ViewOptionDefinition<double>
    {
        public override double Default { get { return 25.0; } }

        public override bool IsValid(ref double proposedValue)
        {
            proposedValue = Math.Min(Math.Max(proposedValue, 3.0), 50.0);
            return true;
        }

        public override EditorOptionKey<double> Key { get { return StructureMarginElement.MarginWidthId; } }
    }

    /// <summary>
    /// Helper class to handle the rendering of the structure margin.
    /// </summary>
    class StructureMarginElement : FrameworkElement
    {
        private readonly IWpfTextView textView;
        private readonly IVerticalScrollBar scrollBar;

        private ITagAggregator<IBlockTag> tagger;
        private BlockColoring blockColoring;
        private Brush methodBrush;
        private double lineWidth;
        private double gapWidth;

        const double MarginWidth = 25.0;

        public static readonly EditorOptionKey<Color> ClassColorId = new EditorOptionKey<Color>("StructureMargin/ClassColor");
        public static readonly EditorOptionKey<Color> ConditionalColorId = new EditorOptionKey<Color>("StructureMargin/ConditionalColor");
        public static readonly EditorOptionKey<Color> LoopColorId = new EditorOptionKey<Color>("StructureMargin/LoopColor");
        public static readonly EditorOptionKey<Color> MethodColorId = new EditorOptionKey<Color>("StructureMargin/MethodColor");
        public static readonly EditorOptionKey<Color> UnknownColorId = new EditorOptionKey<Color>("StructureMargin/UnknownColor");
        public static readonly EditorOptionKey<double> LineWidthId = new EditorOptionKey<double>("StructureMargin/LineWidth");
        public static readonly EditorOptionKey<double> GapWidthId = new EditorOptionKey<double>("StructureMargin/GapWidth");

        public static readonly EditorOptionKey<Color> MethodEllipseColorId = new EditorOptionKey<Color>("StructureMargin/MethodEllipseColor");
        public static readonly EditorOptionKey<double> MarginWidthId = new EditorOptionKey<double>("StructureMargin/MarginWidth");
        public static readonly EditorOptionKey<bool> EnabledOptionId = new EditorOptionKey<bool>("StructureMargin/MarginEnabled");

        /// <summary>
        /// Constructor for the StructureMarginElement.
        /// </summary>
        /// <param name="textView">ITextView to which this StructureMargenElement will be attacheded.</param>
        /// <param name="verticalScrollbar">Vertical scrollbar of the ITextViewHost that contains <paramref name="textView"/>.</param>
        /// <param name="tagFactory">MEF tag factory.</param>
        public StructureMarginElement(IWpfTextView textView, IVerticalScrollBar verticalScrollbar, StructureMarginFactory factory)
        {

            factory.LoadOption(textView.Options, StructureMarginElement.EnabledOptionId.Name);
            factory.LoadOption(textView.Options, StructureMarginElement.MarginWidthId.Name);
            factory.LoadOption(textView.Options, StructureMarginElement.MethodEllipseColorId.Name);

            factory.LoadOption(textView.Options, StructureMarginElement.ClassColorId.Name);
            factory.LoadOption(textView.Options, StructureMarginElement.ConditionalColorId.Name);
            factory.LoadOption(textView.Options, StructureMarginElement.LoopColorId.Name);
            factory.LoadOption(textView.Options, StructureMarginElement.MethodColorId.Name);
            factory.LoadOption(textView.Options, StructureMarginElement.UnknownColorId.Name);

            factory.LoadOption(textView.Options, StructureMarginElement.LineWidthId.Name);
            factory.LoadOption(textView.Options, StructureMarginElement.GapWidthId.Name);

            this.textView = textView;
            this.scrollBar = verticalScrollbar;

            this.SnapsToDevicePixels = true;

            this.gapWidth = textView.Options.GetOptionValue(StructureMarginElement.GapWidthId);
            this.lineWidth = textView.Options.GetOptionValue(StructureMarginElement.LineWidthId);

            this.blockColoring = new BlockColoring(this.lineWidth,
                                                   textView.Options.GetOptionValue(StructureMarginElement.ClassColorId),
                                                   textView.Options.GetOptionValue(StructureMarginElement.ConditionalColorId),
                                                   textView.Options.GetOptionValue(StructureMarginElement.LoopColorId),
                                                   textView.Options.GetOptionValue(StructureMarginElement.MethodColorId),
                                                   textView.Options.GetOptionValue(StructureMarginElement.UnknownColorId));

            this.tagger = factory.TagAggregatorFactoryService.CreateTagAggregator<IBlockTag>(textView);

            Color methodColor = textView.Options.GetOptionValue(StructureMarginElement.MethodEllipseColorId);
            if (methodColor.A != 0)
            {
                this.methodBrush = new SolidColorBrush(methodColor);
                this.methodBrush.Freeze();
            }

            //Make our width big enough to see, but not so big that it consumes a lot of
            //real-estate.
            this.Width = textView.Options.GetOptionValue(StructureMarginElement.MarginWidthId);

            this.OnOptionsChanged(null, null);
            this.textView.Options.OptionChanged += this.OnOptionsChanged;

            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    //Hook up to the various events we need to keep the caret margin current.
                    this.tagger.BatchedTagsChanged += OnTagsChanged;
                    this.scrollBar.TrackSpanChanged += OnTrackSpanChanged;

                    //Force the margin to be rerendered since things might have changed while the margin was hidden.
                    this.InvalidateVisual();
                }
                else
                {
                    this.tagger.BatchedTagsChanged -= OnTagsChanged;
                    this.scrollBar.TrackSpanChanged -= OnTrackSpanChanged;
                }
            };
        }

        public void Dispose()
        {
            this.textView.Options.OptionChanged -= this.OnOptionsChanged;

            this.tagger.Dispose();
        }

        public bool UpdateTip(IOverviewMargin margin, MouseEventArgs e, ToolTip tip)
        {
            if (!this.textView.IsClosed)
            {
                Point pt = e.GetPosition(this);
                if ((pt.X >= 0.0) && (pt.X <= MarginWidth))
                {
                    SnapshotPoint position = this.scrollBar.GetBufferPositionOfYCoordinate(pt.Y);

                    IBlockTag deepestTag = null;
                    var tags = this.tagger.GetTags(new SnapshotSpan(position, 0));
                    foreach (var tagSpan in tags)
                    {
                        if (tagSpan.Tag.Type != BlockType.Unknown)
                        {
                            if ((deepestTag == null) || (tagSpan.Tag.Level > deepestTag.Level))
                                deepestTag = tagSpan.Tag;
                        }
                    }

                    if (deepestTag != null)
                    {
                        tip.IsOpen = true;

                        FrameworkElement context = deepestTag.Context(this.blockColoring,
                                                                      this.textView.FormattedLineSource.DefaultTextProperties.Typeface,
                                                                      this.textView.FormattedLineSource.DefaultTextProperties.FontRenderingEmSize);

                        //The width of the view is in zoomed coordinates so factor the zoom factor into the tip window width computation.
                        double zoom = this.textView.ZoomLevel / 100.0;
                        tip.MinWidth = tip.MaxWidth = Math.Floor(Math.Max(50.0, this.textView.ViewportWidth * zoom * 0.5));
                        tip.MinHeight = tip.MaxHeight = context.Height + 12.0;

                        tip.Content = context;

                        return true;
                    }
                }
            }

            return false;
        }

        public bool Enabled
        {
            get
            {
                return this.textView.Options.GetOptionValue<bool>(StructureMarginElement.EnabledOptionId);
            }
        }

        private void OnOptionsChanged(object sender, EventArgs e)
        {
            this.Visibility = this.Enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTagsChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        private void OnTrackSpanChanged(object sender, EventArgs e)
        {
            //Force the visual to invalidate since the we'll need to redraw all the structure markings
            this.InvalidateVisual();
        }

        /// <summary>
        /// Override for the FrameworkElement's OnRender. When called, redraw
        /// all of the markers 
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!this.textView.IsClosed)
            {
                var tags = this.tagger.GetTags(new SnapshotSpan(this.textView.TextSnapshot, 0, this.textView.TextSnapshot.Length));
                foreach (var tag in tags)
                {
                    this.DrawBlock(drawingContext, tag);
                }
            }
        }

        private void DrawBlock(DrawingContext drawingContext, IMappingTagSpan<IBlockTag> tag)
        {
            if ((tag.Tag.Type != BlockType.Namespace) && (tag.Tag.Type != BlockType.Root))
            {
                NormalizedSnapshotSpanCollection spans = tag.Span.GetSpans(this.textView.TextSnapshot);
                foreach (var span in spans)
                {
                    double x = (double)(tag.Tag.Level - 1) * (this.lineWidth + this.gapWidth) + 1.0;

                    if (x < this.ActualWidth)
                    {
                        double yTop = this.scrollBar.GetYCoordinateOfBufferPosition(span.Start);
                        double yBottom = this.scrollBar.GetYCoordinateOfBufferPosition(span.End);

                        if (yBottom > yTop + 2.0)
                        {
                            if ((tag.Tag.Type == BlockType.Method) && (this.methodBrush != null))
                            {
                                drawingContext.PushClip(new RectangleGeometry(new Rect(x, 0.0, (this.ActualWidth - x), this.ActualHeight)));
                                drawingContext.DrawEllipse(this.methodBrush, null, new Point(x, (yTop + yBottom) * 0.5),
                                                           (this.ActualWidth - x), (yBottom - yTop) * 0.5);
                                drawingContext.Pop();
                            }

                            drawingContext.DrawLine(this.blockColoring.GetPen(tag.Tag),
                                                    new Point(x, yTop),
                                                    new Point(x, yBottom));
                        }
                    }
                }
            }
        }
    }

}
