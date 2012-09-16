// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Outlining;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(EditorOptionDefinition))]
    public sealed class ElisionColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Color.FromArgb(0x40, 0xff, 0x4f, 0x4f); } }

        public override EditorOptionKey<Color> Key { get { return OverviewMargin.ElisionColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    public sealed class OffScreenColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Color.FromArgb(0x30, 0x00, 0x00, 0x00); } }

        public override EditorOptionKey<Color> Key { get { return OverviewMargin.OffScreenColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    public sealed class VisibleColor : ViewOptionDefinition<Color>
    {
        public override Color Default { get { return Color.FromArgb(0x00, 0xff, 0xff, 0xff); } }

        public override EditorOptionKey<Color> Key { get { return OverviewMargin.VisibleColorId; } }
    }

    /// <summary>
    /// Manages the logical content of the OverviewMargin, which displays information
    /// relative to the entire document (optionally including elided regions) and supports
    /// click navigation.
    /// </summary>
    internal class OverviewMargin : ContainerMargin, IOverviewMargin, IOverviewTipFactory
    {
        #region Private Members
        const double VerticalPadding = 1.0;
        const double LeftPadding = 4.0;
        const double RightPadding = 3.0;
        const double MinViewportHeight = 5.0; // smallest that viewport extent will be drawn
        const double TipWindowDelay = 200.0;

        private readonly Brush _elisionBrush; // background of elided regions
        private readonly Brush _offScreenBrush; // background for document areas outside of viewport extent
        private readonly Brush _visibleBrush; // background for document areas inside of viewport extent

        private readonly IOutliningManager _outliningManager;

        private SimpleScrollBar _scrollBar;

        private DateTime? _showTipWhen;
        private ToolTip _tipWindow;
        private IWpfTextView _tipView;
        private IProjectionBuffer _tipBuffer;
        private OverviewMarginProvider _provider;

        IList<Lazy<IOverviewTipFactoryProvider, ITipMetadata>> _orderedTipFactoryProviders = new List<Lazy<IOverviewTipFactoryProvider, ITipMetadata>>();
        private IList<IOverviewTipFactory> _tipFactories;

        public static readonly EditorOptionKey<Color> ElisionColorId = new EditorOptionKey<Color>("OverviewMarginImpl/ElisionColor");
        public static readonly EditorOptionKey<Color> OffScreenColorId = new EditorOptionKey<Color>("OverviewMarginImpl/OffScreenColor");
        public static readonly EditorOptionKey<Color> VisibleColorId = new EditorOptionKey<Color>("OverviewMarginImpl/VisibleColor");

        /// <summary>
        /// Constructor for the OverviewMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        private OverviewMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin, OverviewMarginProvider myProvider)
            : base(PredefinedOverviewMarginNames.Overview, Orientation.Vertical, textViewHost, myProvider.OrderedMarginProviders)
        {
            _provider = myProvider;

            _provider.LoadOption(base.TextViewHost.TextView.Options, DefaultOverviewMarginOptions.ExpandElisionsInOverviewMarginId.Name);
            _provider.LoadOption(base.TextViewHost.TextView.Options, DefaultOverviewMarginOptions.PreviewSizeId.Name);

            _provider.LoadOption(base.TextViewHost.TextView.Options, OverviewMargin.ElisionColorId.Name);
            _provider.LoadOption(base.TextViewHost.TextView.Options, OverviewMargin.OffScreenColorId.Name);
            _provider.LoadOption(base.TextViewHost.TextView.Options, OverviewMargin.VisibleColorId.Name);

            _outliningManager = myProvider.OutliningManagerService.GetOutliningManager(textViewHost.TextView);

            _scrollBar = new SimpleScrollBar(textViewHost, containerMargin, myProvider._scrollMapFactory, this, !base.TextViewHost.TextView.Options.GetOptionValue(DefaultOverviewMarginOptions.ExpandElisionsInOverviewMarginId));

            _elisionBrush = this.GetBrush(OverviewMargin.ElisionColorId);
            _offScreenBrush = this.GetBrush(OverviewMargin.OffScreenColorId);
            _visibleBrush = this.GetBrush(OverviewMargin.VisibleColorId);

            base.Background = Brushes.Transparent;
            base.ClipToBounds = true;

            {
                var viewRoles = this.TextViewHost.TextView.Roles;
                foreach (var tipProvider in myProvider.OrderedTipProviders)
                {
                    if (viewRoles.ContainsAny(tipProvider.Metadata.TextViewRoles))
                    {
                        _orderedTipFactoryProviders.Add(tipProvider);
                    }
                }
            }

            base.TextViewHost.TextView.Options.OptionChanged += this.OnOptionsChanged;
        }

        private Brush GetBrush(EditorOptionKey<Color> key)
        {
            Brush brush = null;

            Color color = base.TextViewHost.TextView.Options.GetOptionValue(key);
            if (color.A != 0)
            {
                brush = new SolidColorBrush(color);
                brush.Freeze();
            }

            return brush;
        }

        protected override void Close()
        {
            base.TextViewHost.TextView.Options.OptionChanged -= this.OnOptionsChanged;
            UnregisterEvents();

            this.CloseTip();
            if (_tipView != null)
            {
                _tipView.Close();
                _tipView = null;
            }

            base.Close();
        }

        #endregion

        /// <summary>
        /// Factory for the OverviewMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        /// <param name="myProvider">Will be queried for various imported components.</param>
        public static OverviewMargin Create(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin, OverviewMarginProvider myProvider)
        {
            OverviewMargin margin = new OverviewMargin(textViewHost, containerMargin, myProvider);
            margin.Initialize();

            return margin;
        }

        #region IOverviewMargin members
        public IVerticalScrollBar ScrollBar
        {
            get
            {
                base.ThrowIfDisposed();
                return _scrollBar;
            }
        }
        #endregion

        #region IOverviewTipFactory members
        public bool UpdateTip(IOverviewMargin margin, MouseEventArgs e, ToolTip tip)
        {
            int tipSize = base.TextViewHost.TextView.Options.GetOptionValue(DefaultOverviewMarginOptions.PreviewSizeId);
            if (tipSize != 0)
            {
                Point pt = e.GetPosition(this);

                if (_tipBuffer == null)
                {
                    _tipBuffer = _provider.ProjectionFactory.CreateProjectionBuffer(null, new List<object>(0), ProjectionBufferOptions.None);

                    _tipView = _provider.EditorFactory.CreateTextView(new VacuousTextViewModel(_tipBuffer, base.TextViewHost.TextView.TextViewModel.DataModel),
                                                                      base.TextViewHost.TextView.Roles, _provider.EditorOptionsFactoryService.GlobalOptions);
                    _tipView.Options.SetOptionValue(DefaultTextViewOptions.IsViewportLeftClippedId, false);
                    _tipView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory,
                                base.TextViewHost.TextView.Options.GetOptionValue(DefaultWpfViewOptions.AppearanceCategory));
                }

                if (_tipBuffer.CurrentSnapshot.SpanCount == 0)
                {
                    //Track the entire buffer (we use a projection buffer rather than simply the TextView's TextBuffer because -- when the preview isn't visible --
                    //we don't want to spend time reacting to text changes, etc.).
                    _tipBuffer.InsertSpan(0, base.TextViewHost.TextView.TextSnapshot.CreateTrackingSpan(0, base.TextViewHost.TextView.TextSnapshot.Length, SpanTrackingMode.EdgeInclusive));
                }

                double viewHeight = ((double)tipSize) * _tipView.LineHeight;
                SnapshotPoint position = _scrollBar.GetBufferPositionOfYCoordinate(pt.Y);

                _tipView.DisplayTextLineContainingBufferPosition(new SnapshotPoint(_tipView.TextSnapshot, position.Position), viewHeight * 0.5, ViewRelativePosition.Bottom,
                                                                 null, viewHeight);

                double left = double.MaxValue;
                foreach (ITextViewLine line in _tipView.TextViewLines)
                {
                    //Find the first non-whitespace character on the line
                    for (int i = line.Start.Position; (i < line.End.Position); ++i)
                    {
                        if (!char.IsWhiteSpace(line.Snapshot[i]))
                        {
                            double l = line.GetCharacterBounds(new SnapshotPoint(line.Snapshot, i)).Left;
                            if (l < left)
                                left = l;
                            break;
                        }
                    }
                }

                _tipView.ViewportLeft = (left == double.MaxValue) ? 0.0 : (left * 0.75); //Compress most of the indentation (but leave a little)

                //The width of the view is in zoomed coordinates so factor the zoom factor into the tip window width computation.
                double zoom = base.TextViewHost.TextView.ZoomLevel / 100.0;
                _tipWindow.MinWidth = _tipWindow.MaxWidth = Math.Floor(Math.Max(50.0, base.TextViewHost.TextView.ViewportWidth * zoom * 0.5));
                _tipWindow.MinHeight = _tipWindow.MaxHeight = 8.0 + viewHeight;

                _tipWindow.IsOpen = true;
                _tipWindow.Content = _tipView.VisualElement;

                return true;
            }

            return false;
        }
        #endregion

        // RegisterEvents() will be called for the first time from Initialize()
        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            base.TextViewHost.TextView.LayoutChanged += OnLayoutChanged;
            _scrollBar.TrackSpanChanged += OnTrackSpanChanged;

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseRightButtonDown += OnMouseRightButtonDown;

            this.MouseMove += OnMouseMove;
            this.MouseEnter += OnMouseEnter;
            this.MouseLeave += OnMouseLeave;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.TextViewHost.TextView.TextDataModel.ContentTypeChanged += OnContentTypeChanged;

        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            base.TextViewHost.TextView.LayoutChanged -= OnLayoutChanged;
            _scrollBar.TrackSpanChanged -= OnTrackSpanChanged;

            this.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            this.MouseRightButtonDown -= OnMouseRightButtonDown;

            this.MouseMove -= OnMouseMove;
            this.MouseEnter -= OnMouseEnter;
            this.MouseLeave -= OnMouseLeave;
            this.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            this.TextViewHost.TextView.TextDataModel.ContentTypeChanged -= OnContentTypeChanged;
        }

        #region Event Handlers

        void OnContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e)
        {
            _tipFactories = null;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            this.InvalidateVisual();
        }

        private void OnTrackSpanChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        protected void OnOptionsChanged(object sender, EditorOptionChangedEventArgs e)
        {
            // Note there is NOT an option for OverviewMarginEnabled.  This is because if the overview margin
            // has no children, it is inactive.

            var options = base.TextViewHost.TextView.Options;
            if (DefaultOverviewMarginOptions.ExpandElisionsInOverviewMarginId.Name.Equals(e.OptionId))
            {
                _scrollBar.UseElidedCoordinates = !options.GetOptionValue(DefaultOverviewMarginOptions.ExpandElisionsInOverviewMarginId); //This will generate a track changed event, invalidating the view.
            }
            else if (DefaultOverviewMarginOptions.PreviewSizeId.Name.Equals(e.OptionId))
            {
                if (_tipFactories != null)
                {
                    if (options.GetOptionValue(DefaultOverviewMarginOptions.PreviewSizeId) != 0)
                    {
                        if ((_tipFactories.Count == 0) || (_tipFactories[_tipFactories.Count - 1] != this))
                        {
                            _tipFactories.Add(this);
                        }
                    }
                    else
                    {
                        if ((_tipFactories.Count > 0) && (_tipFactories[_tipFactories.Count - 1] == this))
                        {
                            _tipFactories.RemoveAt(_tipFactories.Count - 1);
                        }
                    }
                }
            }

            // TODO: handle margin show/hide status -> (Un)RegisterEvents()
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.CaptureMouse();

            this.CloseTip();

            Point pt = e.GetPosition(this);
            this.ScrollViewToYCoordinate(pt.Y, e.ClickCount == 2);
        }

        void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.CloseTip();

            if (this.ContextMenu == null)
            {
                ContextMenu context = new ContextMenu();

                AddBoolEntry(context, DefaultOverviewMarginOptions.ExpandElisionsInOverviewMarginId, Strings.ExpandElisions);
                AddPreviewEntry(context, DefaultOverviewMarginOptions.PreviewSizeId, Strings.ShowTip);

                context.Closed += delegate { this.ContextMenu = null; };
                this.ContextMenu = context;
            }
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
        }

        void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _showTipWhen = DateTime.Now.AddMilliseconds(TipWindowDelay);
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.IsMouseCaptured)
            {
                Point pt = e.GetPosition(this);
                this.ScrollViewToYCoordinate(pt.Y, false);
            }
            else if (_showTipWhen.HasValue && (DateTime.Now > _showTipWhen.Value))
            {
                this.OpenTip(e);
            }
        }

        void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _showTipWhen = null;
            this.CloseTip();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Don't bother drawing if we have no content or no area to draw in (implying there are no children)
            if (ActualWidth > 0.0)
            {
                RenderViewportExtent(drawingContext);
                if (!_scrollBar.UseElidedCoordinates)
                {
                    RenderElidedRegions(drawingContext);
                }
            }
        }
        #endregion

        private void AddBoolEntry(ContextMenu context, EditorOptionKey<bool> key, string label)
        {
            MenuItem item = new MenuItem();
            item.IsCheckable = true;
            item.IsChecked = base.TextViewHost.TextView.Options.GetOptionValue(key);
            item.Header = label;
            item.Click += delegate
            {
                base.TextViewHost.TextView.Options.SetOptionValue(key, item.IsChecked);
                _provider.SaveOption(base.TextViewHost.TextView.Options, key.Name);
            };
            context.Items.Add(item);
        }

        private void AddPreviewEntry(ContextMenu context, EditorOptionKey<int> key, string label)
        {
            MenuItem item = new MenuItem();
            item.IsCheckable = true;
            item.IsChecked = base.TextViewHost.TextView.Options.GetOptionValue(key) != 0;
            item.Header = label;
            item.Click += delegate
            {
                base.TextViewHost.TextView.Options.SetOptionValue(key, item.IsChecked ? 7 : 0);
                _provider.SaveOption(base.TextViewHost.TextView.Options, key.Name);
            };
            context.Items.Add(item);
        }

        private void OpenTip(MouseEventArgs e)
        {
            this.EnsureTipFactories();
            if (_tipFactories.Count > 0)
            {
                if (_tipWindow == null)
                {
                    _tipWindow = new ToolTip();

                    _tipWindow.ClipToBounds = true;

                    _tipWindow.Placement = PlacementMode.Left;
                    _tipWindow.PlacementTarget = base.TextViewHost.TextView.VisualElement;

                    _tipWindow.HorizontalContentAlignment = HorizontalAlignment.Left;

                    _tipWindow.VerticalAlignment = VerticalAlignment.Top;
                    _tipWindow.VerticalContentAlignment = VerticalAlignment.Top;

                    _tipWindow.HorizontalOffset = 0.0;
                    _tipWindow.VerticalOffset = 0.0;
                }

                //Compensate for zoom since the placement rectangle is specified in terms of the view's coordinate system and not
                //the margin's (which doesn't zoomed).
                double zoom = base.TextViewHost.TextView.ZoomLevel / 100.0;
                _tipWindow.PlacementRectangle = new Rect(base.TextViewHost.TextView.ViewportRight, e.GetPosition(this).Y / zoom, 0.0, 0.0);

                foreach (var tipFactory in _tipFactories)
                {
                    if (tipFactory.UpdateTip(this, e, _tipWindow))
                    {
                        return;
                    }
                }

                this.CloseTip();
            }
        }

        private void EnsureTipFactories()
        {
            if (_tipFactories == null)
            {
                _tipFactories = new List<IOverviewTipFactory>();

                foreach (var tipFactoryProvider in _orderedTipFactoryProviders)
                {
                    foreach (string contentType in tipFactoryProvider.Metadata.ContentTypes)
                    {
                        if (this.TextViewHost.TextView.TextDataModel.ContentType.IsOfType(contentType))
                        {
                            var factory = tipFactoryProvider.Value.GetOverviewTipFactory(this, this.TextViewHost.TextView);
                            if (factory != null)
                                _tipFactories.Add(factory);

                            break;
                        }
                    }
                }

                int tipSize = base.TextViewHost.TextView.Options.GetOptionValue(DefaultOverviewMarginOptions.PreviewSizeId);
                if (tipSize != 0)
                    _tipFactories.Add(this);
            }
        }

        private void CloseTip()
        {
            if (_tipWindow != null)
            {
                _tipWindow.Content = null;
                _tipWindow.IsOpen = false;
                _tipWindow = null;

                if (_tipBuffer != null)
                {
                    //Stop tracking the view's buffer.
                    _tipBuffer.DeleteSpans(0, _tipBuffer.CurrentSnapshot.SpanCount);
                }
            }
        }

        /// <summary>
        /// Scroll the view so that the location corresponding to the specified coordinate
        /// is at the center of the screen.
        /// </summary>
        /// <param name="y">A pixel coordinate relative to the top of the margin.</param>
        /// <remarks>
        /// The corresponding buffer position will be displayed at the center of the viewport.
        /// If the pixel coordinate corresponds to a position beyond the end of the buffer,
        /// the last line of text will be scrolled proportionally higher than center, until
        /// only one line of text is visible.
        /// </remarks>
        internal void ScrollViewToYCoordinate(double y, bool expand)
        {
            double yLastLine = _scrollBar.TrackSpanBottom - _scrollBar.ThumbHeight;
            if (y < yLastLine)
            {
                SnapshotPoint position = _scrollBar.GetBufferPositionOfYCoordinate(y);

                if (expand)
                    this.Expand(position);
                base.TextViewHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(position, 0), EnsureSpanVisibleOptions.AlwaysCenter);
            }
            else
            {
                // Place the last line of the document somewhere between the top of the view and the center of the view,
                // depending on how far below the last mapped coordinate the user clicked.  The lowest point of interest
                // is with the thumb at the bottom of the track, corresponding to a click half a thumbheight above bottom.
                y = Math.Min(y, yLastLine + (_scrollBar.ThumbHeight / 2.0));
                double fraction = (y - yLastLine) / _scrollBar.ThumbHeight; // 0 to 0.5 
                double dyDistanceFromTopOfViewport = base.TextViewHost.TextView.ViewportHeight * (0.5 - fraction);
                SnapshotPoint end = new SnapshotPoint(base.TextViewHost.TextView.TextSnapshot, base.TextViewHost.TextView.TextSnapshot.Length);

                if (expand)
                    this.Expand(end);
                base.TextViewHost.TextView.DisplayTextLineContainingBufferPosition(end, dyDistanceFromTopOfViewport, ViewRelativePosition.Top);
            }
        }

        private void Expand(SnapshotPoint position)
        {
            if (_outliningManager != null)
            {
                _outliningManager.ExpandAll(new SnapshotSpan(position, 0), (collapsible) =>
                {
                    Span s = collapsible.Extent.GetSpan(position.Snapshot);
                    return (position > s.Start) && (position < s.End);
                });
            }
        }

        #region Rendering
        /// <summary>
        /// Draw all the elided regions
        /// </summary>
        private void RenderElidedRegions(DrawingContext drawingContext)
        {
            if (_elisionBrush != null)
            {
                NormalizedSnapshotSpanCollection unelidedSourceSpans =
                        base.TextViewHost.TextView.BufferGraph.MapDownToSnapshot(
                            new SnapshotSpan(base.TextViewHost.TextView.VisualSnapshot, 0, base.TextViewHost.TextView.VisualSnapshot.Length),
                            SpanTrackingMode.EdgeInclusive,
                            base.TextViewHost.TextView.TextSnapshot);

                double yBottom = _scrollBar.GetYCoordinateOfBufferPosition(new SnapshotPoint(base.TextViewHost.TextView.TextSnapshot, 0));
                foreach (var span in unelidedSourceSpans)
                {
                    double yTop = _scrollBar.GetYCoordinateOfBufferPosition(span.Start);
                    DrawRectangle(drawingContext, _elisionBrush, this.ActualWidth, yBottom, yTop);

                    yBottom = _scrollBar.GetYCoordinateOfBufferPosition(span.End);
                }

                double y = _scrollBar.GetYCoordinateOfBufferPosition(new SnapshotPoint(base.TextViewHost.TextView.TextSnapshot, base.TextViewHost.TextView.TextSnapshot.Length)) - _scrollBar.ThumbHeight;
                DrawRectangle(drawingContext, _elisionBrush, this.ActualWidth, yBottom, y);
            }
        }

        /// <summary>
        /// Shade the visible/offScreen portion of the buffer
        /// </summary>
        private void RenderViewportExtent(DrawingContext drawingContext)
        {
            var tvl = base.TextViewHost.TextView.TextViewLines;
            SnapshotPoint start = new SnapshotPoint(tvl.FirstVisibleLine.Snapshot, tvl.FirstVisibleLine.Start);

            double viewportTop = Math.Floor(_scrollBar.GetYCoordinateOfBufferPosition(start));
            double viewportBottom = Math.Ceiling(Math.Max(GetYCoordinateOfLineBottom(tvl.LastVisibleLine), viewportTop + MinViewportHeight));

            DrawRectangle(drawingContext, _offScreenBrush, this.ActualWidth, _scrollBar.TrackSpanTop, viewportTop);

            DrawRectangle(drawingContext, _visibleBrush, this.ActualWidth, viewportTop, viewportBottom);

            DrawRectangle(drawingContext, _offScreenBrush, this.ActualWidth, viewportBottom, _scrollBar.TrackSpanBottom);
        }

        private static void DrawRectangle(DrawingContext drawingContext, Brush brush, double width, double yTop, double yBottom)
        {
            if ((brush != null) && (yBottom - VerticalPadding > yTop))
                drawingContext.DrawRectangle(brush, null, new Rect(0.0, yTop, width, yBottom - yTop));
        }

        /// <summary>
        /// Get the scrollbar y coordinate of the bottom of the line.  Generally that will
        /// be the top of the next line, but if there's no next line, fake it
        /// based on the proportion of empty space below the last line.
        /// </summary>
        /// <param name="line">snapshot line number; the line must be visible</param>
        private double GetYCoordinateOfLineBottom(ITextViewLine line)
        {
            var snapshot = base.TextViewHost.TextView.TextSnapshot;
            if (line.EndIncludingLineBreak.Position < snapshot.Length)
            {
                // line is not the last line; get the Y coordinate of the next line.
                return _scrollBar.GetYCoordinateOfBufferPosition(new SnapshotPoint(snapshot, line.EndIncludingLineBreak.Position + 1));
            }
            else
            {
                // last line.
                var tvl = base.TextViewHost.TextView.TextViewLines;
                double empty = 1 - ((tvl.LastVisibleLine.Bottom - tvl.FirstVisibleLine.Bottom) / base.TextViewHost.TextView.ViewportHeight);
                return _scrollBar.GetYCoordinateOfScrollMapPosition(_scrollBar.Map.End + _scrollBar.Map.ThumbSize * empty);
            }
        }

        #endregion

        /// <summary>
        /// A scrollbar that can be switched to either delegate to the real (view-based) scrollbar or
        /// to use a specified scroll map.
        /// </summary>
        private class SimpleScrollBar : IVerticalScrollBar
        {
            IScrollMapFactoryService _scrollMapFactory;
            private ScrollMapWrapper _scrollMap = new ScrollMapWrapper();
            private IWpfTextView _textView;
            private IWpfTextViewMargin _realScrollBarMargin;
            private IVerticalScrollBar _realScrollBar;
            private bool _useElidedCoordinates = false;

            double _trackSpanTop;
            double _trackSpanBottom;

            private class ScrollMapWrapper : IScrollMap
            {
                private IScrollMap _scrollMap;

                public ScrollMapWrapper()
                {
                }

                public IScrollMap ScrollMap
                {
                    get { return _scrollMap; }
                    set
                    {
                        if (_scrollMap != null)
                        {
                            _scrollMap.MappingChanged -= OnMappingChanged;
                        }

                        _scrollMap = value;

                        _scrollMap.MappingChanged += OnMappingChanged;

                        this.OnMappingChanged(this, new EventArgs());
                    }
                }

                void OnMappingChanged(object sender, EventArgs e)
                {
                    EventHandler handler = this.MappingChanged;
                    if (handler != null)
                        handler(sender, e);
                }

                public double GetCoordinateAtBufferPosition(SnapshotPoint bufferPosition)
                {
                    return _scrollMap.GetCoordinateAtBufferPosition(bufferPosition);
                }

                public bool AreElisionsExpanded
                {
                    get { return _scrollMap.AreElisionsExpanded; }
                }

                public SnapshotPoint GetBufferPositionAtCoordinate(double coordinate)
                {
                    return _scrollMap.GetBufferPositionAtCoordinate(coordinate);
                }

                public double Start
                {
                    get { return _scrollMap.Start; }
                }

                public double End
                {
                    get { return _scrollMap.End; }
                }

                public double ThumbSize
                {
                    get { return _scrollMap.ThumbSize; }
                }

                public ITextView TextView
                {
                    get { return _scrollMap.TextView; }
                }

                public double GetFractionAtBufferPosition(SnapshotPoint bufferPosition)
                {
                    return _scrollMap.GetFractionAtBufferPosition(bufferPosition);
                }

                public SnapshotPoint GetBufferPositionAtFraction(double fraction)
                {
                    return _scrollMap.GetBufferPositionAtFraction(fraction);
                }

                public event EventHandler MappingChanged;
            }

            /// <summary>
            /// If true, map to the view's scrollbar; else map to the scrollMap.
            /// </summary>
            public bool UseElidedCoordinates
            {
                get { return _useElidedCoordinates; }
                set
                {
                    if (value != _useElidedCoordinates)
                    {
                        _useElidedCoordinates = value;
                        this.ResetScrollMap();
                    }
                }
            }

            private void ResetScrollMap()
            {
                if (_useElidedCoordinates && this.UseRealScrollBarTrackSpan)
                {
                    _scrollMap.ScrollMap = _realScrollBar.Map;
                }
                else
                {
                    _scrollMap.ScrollMap = _scrollMapFactory.Create(_textView, !_useElidedCoordinates);
                }
            }

            private void ResetTrackSpan()
            {
                if (this.UseRealScrollBarTrackSpan)
                {
                    _trackSpanTop = _realScrollBar.TrackSpanTop;
                    _trackSpanBottom = _realScrollBar.TrackSpanBottom;
                }
                else
                {
                    _trackSpanTop = 0.0;
                    _trackSpanBottom = _textView.ViewportHeight;
                }

                //Ensure that the length of the track span is never 0.
                _trackSpanBottom = Math.Max(_trackSpanTop + 1.0, _trackSpanBottom);
            }

            private bool UseRealScrollBarTrackSpan
            {
                get
                {
                    try
                    {
                        return (_realScrollBar != null) && (_realScrollBarMargin != null) && (_realScrollBarMargin.VisualElement.Visibility == Visibility.Visible);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            void OnMappingChanged(object sender, EventArgs e)
            {
                this.RaiseTrackChangedEvent();
            }

            private void RaiseTrackChangedEvent()
            {
                EventHandler handler = this.TrackSpanChanged;
                if (handler != null)
                    handler(this, new EventArgs());
            }

            public SimpleScrollBar(IWpfTextViewHost host, IWpfTextViewMargin containerMargin, IScrollMapFactoryService scrollMapFactory, FrameworkElement container, bool useElidedCoordinates)
            {
                _textView = host.TextView;

                _realScrollBarMargin = containerMargin.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar) as IWpfTextViewMargin;
                if (_realScrollBarMargin != null)
                {
                    _realScrollBar = _realScrollBarMargin as IVerticalScrollBar;
                    if (_realScrollBar != null)
                    {
                        _realScrollBarMargin.VisualElement.IsVisibleChanged += OnScrollBarIsVisibleChanged;
                        _realScrollBar.TrackSpanChanged += OnScrollBarTrackSpanChanged;
                    }
                }
                this.ResetTrackSpan();

                _scrollMapFactory = scrollMapFactory;
                _useElidedCoordinates = useElidedCoordinates;
                this.ResetScrollMap();

                _scrollMap.MappingChanged += delegate { this.RaiseTrackChangedEvent(); };

                container.SizeChanged += OnContainerSizeChanged;
            }

            void OnScrollBarIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                this.ResetTrackSpan();

                if (_useElidedCoordinates)
                    this.ResetScrollMap();  //This will indirectly cause RaiseTrackChangedEvent to be called.
                else
                    this.RaiseTrackChangedEvent();
            }

            void OnContainerSizeChanged(object sender, EventArgs e)
            {
                if (!this.UseRealScrollBarTrackSpan)
                {
                    this.ResetTrackSpan();
                    this.RaiseTrackChangedEvent();
                }
            }

            void OnScrollBarTrackSpanChanged(object sender, EventArgs e)
            {
                if (this.UseRealScrollBarTrackSpan)
                {
                    this.ResetTrackSpan();
                    this.RaiseTrackChangedEvent();
                }
            }

            #region IVerticalScrollBar Members
            public IScrollMap Map
            {
                get { return _scrollMap; }
            }

            public double GetYCoordinateOfBufferPosition(SnapshotPoint bufferPosition)
            {
                double scrollMapPosition = _scrollMap.GetCoordinateAtBufferPosition(bufferPosition);
                return this.GetYCoordinateOfScrollMapPosition(scrollMapPosition);
            }

            public double GetYCoordinateOfScrollMapPosition(double scrollMapPosition)
            {
                double minimum = _scrollMap.Start;
                double maximum = _scrollMap.End;
                double height = maximum - minimum;

                return this.TrackSpanTop + ((scrollMapPosition - minimum) * this.TrackSpanHeight) / (height + _scrollMap.ThumbSize);
            }

            public SnapshotPoint GetBufferPositionOfYCoordinate(double y)
            {
                double minimum = _scrollMap.Start;
                double maximum = _scrollMap.End;
                double height = maximum - minimum;

                double scrollCoordinate = minimum + (y - this.TrackSpanTop) * (height + _scrollMap.ThumbSize) / this.TrackSpanHeight;

                return _scrollMap.GetBufferPositionAtCoordinate(scrollCoordinate);
            }

            public double TrackSpanTop
            {
                get { return _trackSpanTop; }
            }

            public double TrackSpanBottom
            {
                get { return _trackSpanBottom; }
            }

            public double TrackSpanHeight
            {
                get { return _trackSpanBottom - _trackSpanTop; }
            }

            public double ThumbHeight
            {
                get
                {
                    double minimum = _scrollMap.Start;
                    double maximum = _scrollMap.End;
                    double height = maximum - minimum;

                    return _scrollMap.ThumbSize / (height + _scrollMap.ThumbSize) * this.TrackSpanHeight;
                }
            }

            public event EventHandler TrackSpanChanged;
            #endregion
        }

        internal class VacuousTextViewModel : ITextViewModel
        {
            ITextBuffer buffer;
            private ITextDataModel dataModel;

            public VacuousTextViewModel(ITextBuffer buffer, ITextDataModel dataModel)
            {
                this.buffer = buffer;
                this.dataModel = dataModel;
                this.Properties = new PropertyCollection();
            }

            public ITextDataModel DataModel
            {
                get { return this.dataModel; }
            }

            public ITextBuffer DataBuffer
            {
                get { return this.buffer; }
            }

            public ITextBuffer EditBuffer
            {
                get { return this.buffer; }
            }

            public ITextBuffer VisualBuffer
            {
                get { return this.buffer; }
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public PropertyCollection Properties { get; internal set; }

            public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint)
            {
                // The edit buffer is the same as the visual buffer, so just return the passed-in point.
                return editBufferPoint;
            }

            public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode)
            {
                // The edit buffer is the same as the visual buffer, so just return the passed-in point translated to the correct snapshot.
                return editBufferPoint.TranslateTo(targetVisualSnapshot, trackingMode);
            }

            public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity)
            {
                // The edit buffer is the same as the visual buffer, so just return true.
                return true;
            }
        }

    }
}
