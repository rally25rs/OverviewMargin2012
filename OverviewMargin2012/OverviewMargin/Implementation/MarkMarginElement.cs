// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;

    [Export(typeof(EditorOptionDefinition))]
    public sealed class MarkMarginWidth : ViewOptionDefinition<double>
    {
        public override double Default { get { return 8.0; } }

        public override bool IsValid(ref double proposedValue)
        {
            proposedValue = Math.Min(Math.Max(proposedValue, 3.0), 20.0);
            return true;
        }

        public override EditorOptionKey<double> Key { get { return MarkMarginElement.MarkMarginWidthId; } }
    }

    /// <summary>
    /// Helper class to handle the rendering of the overview mark margin.
    /// </summary>
    /// <remarks>
    /// The containing class (OverviewMarkMargin) handles logical state, such as event
    /// listeners, collecting tags and obtaining marks for them, and such. This class
    /// is only responsible for rendering the marks.
    /// </remarks>
    internal class MarkMarginElement : FrameworkElement
    {
        private readonly IWpfTextView _textView;
        private readonly IVerticalScrollBar _scrollBar;
        private readonly IEditorFormatMap _editorFormatMap;
        private readonly List<IOverviewMarkFactory> _factories;
        private readonly Pen _markPen;
        private IList<Tuple<IOverviewMark, double>> _visibleMarks = new List<Tuple<IOverviewMark, double>>();

        const double MarkHeight = 4.0;
        const double MarkSpacing = 6.0;

        const double MarkOffset = 2.0;

        public static readonly EditorOptionKey<double> MarkMarginWidthId = new EditorOptionKey<double>("OverviewMarginImpl/MarkMarginWidth");

        public MarkMarginElement(IWpfTextView textView, IVerticalScrollBar scrollbar, OverviewMarkMarginProvider provider)
        {
            provider.LoadOption(textView.Options, DefaultOverviewMarginOptions.MarkMarginId.Name);
            provider.LoadOption(textView.Options, MarkMarginWidthId.Name);

            _markPen = new Pen(Brushes.DimGray, 1.0);
            _markPen.Freeze();

            _textView = textView;
            _scrollBar = scrollbar;
            _editorFormatMap = provider.EditorFormatMapService.GetEditorFormatMap(_textView);

            _factories = new List<IOverviewMarkFactory>();
            foreach (var markProvider in provider.MarkProviders)
            {
                IEnumerable<string> providerRoles = markProvider.Metadata.TextViewRoles;
                if (_textView.Roles.ContainsAny(providerRoles))
                {
                    IOverviewMarkFactory factory = markProvider.Value.GetOverviewMarkFactory(textView);
                    if (factory != null)
                    {
                        _factories.Add(factory);
                        factory.MarksChanged += delegate
                        {
                            this.AsynchInvalidateVisual();
                        };
                    }
                }
            }

            //Make our width big enough to see, but not so big that it consumes a lot of
            //real-estate.
            this.Width = textView.Options.GetOptionValue(MarkMarginWidthId);

            this.OnOptionsChanged(null, null);
            textView.Options.OptionChanged += this.OnOptionsChanged;

            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    //Hook up to the various events we need to keep the caret margin current.
                    _scrollBar.TrackSpanChanged += OnTrackSpanChanged;

                    //Force the margin to be rerendered since things might have changed while the margin was hidden.
                    this.AsynchInvalidateVisual();
                }
                else
                {
                    _scrollBar.TrackSpanChanged -= OnTrackSpanChanged;
                }
            };
        }

        private void OnTrackSpanChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        /// <summary>
        /// Asynchronously dispatch an InvalidateVisual on this element. Can be called from a non-UI thread.
        /// </summary>
        public void AsynchInvalidateVisual()
        {
            if (this.IsVisible)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate
                {
                    this.InvalidateVisual();
                    return null;
                }), null);
            }
        }

        public void Dispose()
        {
            _textView.Options.OptionChanged -= this.OnOptionsChanged;
        }

        public bool Enabled
        {
            get
            {
                return _textView.Options.GetOptionValue(DefaultOverviewMarginOptions.MarkMarginId);
            }
        }

        void OnOptionsChanged(object sender, EventArgs e)
        {
            this.Visibility = this.Enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool UpdateTip(IOverviewMargin margin, MouseEventArgs e, ToolTip tip)
        {
            Point pt = e.GetPosition(this);
            if ((pt.X >= 0.0) && (pt.X <= this.Width) && (_visibleMarks.Count > 0))
            {
                foreach (var mark in _visibleMarks)
                {
                    if (Math.Abs(pt.Y - mark.Item2) < MarkHeight * 0.5)
                    {
                        object content = mark.Item1.ToolTipContent;
                        if (content != null)
                        {
                            tip.MinWidth = tip.MinHeight = 0.0;
                            tip.MaxWidth = tip.MaxHeight = double.PositiveInfinity;

                            tip.Content = content;
                            tip.IsOpen = true;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Override for the FrameworkElement's OnRender. When called, redraw
        /// all of the change markers.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            _visibleMarks.Clear();

            DrawMarks(drawingContext, this.GetMarks());
        }

        /// <summary>
        /// Get all the marks in the document, sorted by start point.
        /// </summary>
        /// <returns>A non-null, possibly empty, list.</returns>
        internal IList<Tuple<IOverviewMark, SnapshotPoint>> GetMarks()
        {
            IMappingSpan span;
            if (_scrollBar.Map.AreElisionsExpanded)
            {
                //Display the contents of the elisions.
                span = _textView.BufferGraph.CreateMappingSpan(new SnapshotSpan(_textView.TextSnapshot, 0, _textView.TextSnapshot.Length), SpanTrackingMode.EdgeInclusive);
            }
            else
            {
                //Get just the visible spans.
                span = _textView.BufferGraph.CreateMappingSpan(new SnapshotSpan(_textView.VisualSnapshot, 0, _textView.VisualSnapshot.Length), SpanTrackingMode.EdgeInclusive);
            }

            List<Tuple<IOverviewMark, SnapshotPoint>> marks = new List<Tuple<IOverviewMark, SnapshotPoint>>();
            foreach (var factory in _factories)
            {
                foreach (var mark in factory.GetOverviewMarks(span))
                {
                    SnapshotPoint? position = mark.Position.GetPoint(_textView.TextSnapshot, PositionAffinity.Predecessor);
                    if (position.HasValue)
                        marks.Add(new Tuple<IOverviewMark, SnapshotPoint>(mark, position.Value));
                }
            }

            marks.Sort((left, right) => left.Item2.Position - right.Item2.Position);

            return marks;
        }

        /// <summary>
        /// Draw all the marks in the buffer. 
        /// </summary>
        /// <remarks>
        /// Marks may be represented by glyphs or color swatches,
        /// depending on available space and on the information provided by the mark.
        /// This does not erase any previously rendered marks.
        /// </remarks>
        /// <param name="marks">
        /// A list of overview marks and their associated start positions, sorted by 
        /// ascending start position.
        /// </param>
        private void DrawMarks(DrawingContext drawingContext, IList<Tuple<IOverviewMark, SnapshotPoint>> marks)
        {
            double lastY = double.MinValue;
            foreach (var entry in marks)
            {
                double y = Math.Round(_scrollBar.GetYCoordinateOfBufferPosition(entry.Item2));
                if (y - MarkSpacing > lastY)
                {
                    Brush brush = new SolidColorBrush(entry.Item1.Color);
                    brush.Freeze();

                    Rect rectangle = new Rect(MarkOffset * 0.5, y - MarkHeight * 0.5, this.Width - MarkOffset, MarkHeight);
                    drawingContext.DrawRectangle(brush, _markPen, rectangle);

                    lastY = y;
                }

                _visibleMarks.Add(new Tuple<IOverviewMark, double>(entry.Item1, y));
            }
        }

        private static Brush GetMarkBrush(ResourceDictionary dictionary)
        {
            Brush markBrush = null;
            if (dictionary.Contains(EditorFormatDefinition.ForegroundBrushId))
            {
                markBrush = dictionary[EditorFormatDefinition.ForegroundBrushId] as Brush;
            }
            else if (dictionary.Contains(EditorFormatDefinition.ForegroundColorId))
            {
                markBrush = new SolidColorBrush((Color)dictionary[EditorFormatDefinition.ForegroundColorId]);

            }
            else
            {
                markBrush = Brushes.Blue;
            }

            if (markBrush.CanFreeze)
            {
                markBrush.Freeze();
            }

            return markBrush;
        }
    }
}

