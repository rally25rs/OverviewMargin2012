// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Document;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
    using Microsoft.VisualStudio.Text.Tagging;

    [Export(typeof(EditorOptionDefinition))]
    public sealed class OverviewChangeTrackingMarginWidth : ViewOptionDefinition<double>
    {
        public override double Default { get { return 9.0; } }

        public override bool IsValid(ref double proposedValue)
        {
            proposedValue = Math.Min(Math.Max(proposedValue, 3.0), 20.0);
            return true;
        }

        public override EditorOptionKey<double> Key { get { return ChangeTrackingMarginElement.ChangeTrackingMarginWidthId; } }
    }

    /// <summary>
    /// Helper class to handle the rendering of the change tracking margin.
    /// </summary>
    internal class ChangeTrackingMarginElement : FrameworkElement
    {
        private IWpfTextView _textView;
        private IVerticalScrollBar _scrollBar;
        private IViewTagAggregatorFactoryService _tagAggregatorFactoryService;
        private IEditorFormatMap _editorFormatMap;
        private Brush[] _brushes = new Brush[4];

        internal ITagAggregator<ChangeTag> _changeTagAggregator;

        const double ChangePadding = 3.0;

        const double ChangeOffset = 2.0;

        public static readonly EditorOptionKey<double> ChangeTrackingMarginWidthId = new EditorOptionKey<double>("OverviewMarginImpl/ChangeTrackingMarginWidth");

        /// <summary>
        /// Constructor for the ChangeTrackingMarginElement.
        /// </summary>
        /// <param name="textView">ITextView to which this ChangeTrackingMargenElement will be attached.</param>
        /// <param name="verticalScrollbar">Vertical scrollbar of the ITextViewHost that contains <paramref name="textView"/>.</param>
        public ChangeTrackingMarginElement(IWpfTextView textView, IVerticalScrollBar verticalScrollbar, OverviewChangeTrackingMarginProvider provider)
        {
            provider.LoadOption(textView.Options, DefaultOverviewMarginOptions.ChangeTrackingMarginId.Name);
            provider.LoadOption(textView.Options, ChangeTrackingMarginWidthId.Name);

            _textView = textView;
            _scrollBar = verticalScrollbar;
            _tagAggregatorFactoryService = provider.TagAggregatorFactoryService;
            _editorFormatMap = provider.EditorFormatMapService.GetEditorFormatMap(textView);

            _editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;

            _textView.Closed += (sender, args) => _editorFormatMap.FormatMappingChanged -= OnFormatMappingChanged;

            UpdateBrushes();

            //Make our width big enough to see, but not so big that it consumes a lot of
            //real-estate.
            this.Width = _textView.Options.GetOptionValue(ChangeTrackingMarginWidthId);

            this.OnOptionsChanged(null, null);
            textView.Options.OptionChanged += this.OnOptionsChanged;

            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    //Hook up to the various events we need to keep the changeTracking margin current.
                    _textView.LayoutChanged += OnLayoutChanged;
                    _scrollBar.TrackSpanChanged += OnTrackSpanChanged;

                    this.InvalidateVisual();
                }
                else
                {
                    _textView.LayoutChanged -= OnLayoutChanged;
                    _scrollBar.TrackSpanChanged -= OnTrackSpanChanged;
                }
            };
        }

        public void Dispose()
        {
            this.TrackChanges = false;
            _textView.Options.OptionChanged -= this.OnOptionsChanged;
        }

        public bool Enabled
        {
            get
            {
                return _textView.Options.IsChangeTrackingEnabled() && _textView.Options.GetOptionValue(DefaultOverviewMarginOptions.ChangeTrackingMarginId);
            }
        }

        internal bool TrackChanges
        {
            get
            {
                return (_changeTagAggregator != null);
            }

            set
            {
                if (value != this.TrackChanges)
                {
                    if (value)
                    {
                        //Create a change tagger so we can see what has changed
                        _changeTagAggregator = _tagAggregatorFactoryService.CreateTagAggregator<ChangeTag>(_textView);
                        _changeTagAggregator.TagsChanged += OnTagsChanged;
                    }
                    else
                    {
                        //Dispose of the tagger.
                        _changeTagAggregator.TagsChanged -= OnTagsChanged;
                        _changeTagAggregator.Dispose();
                        _changeTagAggregator = null;
                    }

                    this.InvalidateVisual();
                }
            }
        }

        void OnOptionsChanged(object sender, EventArgs e)
        {
            this.Visibility = this.Enabled ? Visibility.Visible : Visibility.Collapsed;
            this.TrackChanges = this.Enabled;
        }

        void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            if (e.ChangedItems.Contains("Track changes before save") ||
                e.ChangedItems.Contains("Track changes after save") ||
                e.ChangedItems.Contains("Track reverted changes"))
            {
                UpdateBrushes();
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (AnyTextChanges(e.OldViewState.EditSnapshot.Version, e.NewViewState.EditSnapshot.Version))
                this.InvalidateVisual();
        }

        private static bool AnyTextChanges(ITextVersion oldVersion, ITextVersion currentVersion)
        {
            while (oldVersion != currentVersion)
            {
                if (oldVersion.Changes.Count > 0)
                    return true;
                oldVersion = oldVersion.Next;
            }

            return false;
        }

        private void OnTrackSpanChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        void OnTagsChanged(object sender, TagsChangedEventArgs e)
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

        /// <summary>
        /// Override for the FrameworkElement's OnRender. When called, redraw
        /// all of the change markers.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (_changeTagAggregator != null)
            {
                NormalizedSnapshotSpanCollection[] allChanges = GetUnifiedChanges(_textView.TextSnapshot,
                                                                                  _changeTagAggregator.GetTags(new SnapshotSpan(_textView.TextSnapshot,
                                                                                                                                0,
                                                                                                                                _textView.TextSnapshot.Length)));

                DrawChange(drawingContext, ChangeTypes.ChangedSinceOpened, allChanges);
                DrawChange(drawingContext, ChangeTypes.ChangedSinceSaved, allChanges);
                DrawChange(drawingContext, ChangeTypes.ChangedSinceOpened | ChangeTypes.ChangedSinceSaved, allChanges);
            }
        }

        internal static NormalizedSnapshotSpanCollection[] GetUnifiedChanges(ITextSnapshot snapshot, IEnumerable<IMappingTagSpan<ChangeTag>> tags)
        {
            List<SnapshotSpan>[] unnormalizedChanges = new List<SnapshotSpan>[4] { null,
                                                                                   new List<SnapshotSpan>(),
                                                                                   new List<SnapshotSpan>(),
                                                                                   new List<SnapshotSpan>()
                                                                                 };
            foreach (IMappingTagSpan<ChangeTag> change in tags)
            {
                unnormalizedChanges[(int)change.Tag.ChangeTypes].AddRange(change.Span.GetSpans(snapshot));
            }

            NormalizedSnapshotSpanCollection[] changes = new NormalizedSnapshotSpanCollection[4];
            for (int i = 1; (i <= 3); ++i)
                changes[i] = new NormalizedSnapshotSpanCollection(unnormalizedChanges[i]);

            return changes;
        }

        private void UpdateBrushes()
        {
            ResourceDictionary resourceDictionary = _editorFormatMap.GetProperties("Track Changes before save");
            if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundColorId))
            {
                Color color = (Color)resourceDictionary[EditorFormatDefinition.BackgroundColorId];

                _brushes[3] = new SolidColorBrush(color);

                if (_brushes[3].CanFreeze)
                    _brushes[3].Freeze();
            }
            else if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundBrushId))
            {
                _brushes[3] = (Brush)resourceDictionary[EditorFormatDefinition.BackgroundBrushId];

                if (_brushes[3].CanFreeze)
                    _brushes[3].Freeze();
            }
            resourceDictionary = _editorFormatMap.GetProperties("Track Changes after save");
            if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundColorId))
            {
                Color color = (Color)resourceDictionary[EditorFormatDefinition.BackgroundColorId];

                _brushes[1] = new SolidColorBrush(color);

                if (_brushes[1].CanFreeze)
                    _brushes[1].Freeze();
            }
            else if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundBrushId))
            {
                _brushes[1] = (Brush)resourceDictionary[EditorFormatDefinition.BackgroundBrushId];

                if (_brushes[1].CanFreeze)
                    _brushes[1].Freeze();
            }
            resourceDictionary = _editorFormatMap.GetProperties("Track reverted changes");
            if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundColorId))
            {
                Color color = (Color)resourceDictionary[EditorFormatDefinition.BackgroundColorId];

                _brushes[2] = new SolidColorBrush(color);

                if (_brushes[2].CanFreeze)
                    _brushes[2].Freeze();
            }
            else if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundBrushId))
            {
                _brushes[2] = (Brush)resourceDictionary[EditorFormatDefinition.BackgroundBrushId];

                if (_brushes[2].CanFreeze)
                    _brushes[2].Freeze();
            }
        }

        private void DrawChange(DrawingContext drawingContext, ChangeTypes type, NormalizedSnapshotSpanCollection[] allChanges)
        {
            NormalizedSnapshotSpanCollection changes = allChanges[(int)type];
            if (changes.Count > 0)
            {
                double yTop = Math.Floor(_scrollBar.GetYCoordinateOfBufferPosition(changes[0].Start)) - ChangePadding;
                double yBottom = Math.Ceiling(_scrollBar.GetYCoordinateOfBufferPosition(changes[0].End)) + ChangePadding;

                for (int i = 1; (i < changes.Count); ++i)
                {
                    double y = _scrollBar.GetYCoordinateOfBufferPosition(changes[i].Start) - ChangePadding;
                    if (yBottom < y)
                    {
                        drawingContext.DrawRectangle(_brushes[(int)type], null,
                                                     new Rect(ChangeOffset * 0.5, yTop, this.Width - ChangeOffset, yBottom - yTop));

                        yTop = y;
                    }

                    yBottom = Math.Ceiling(_scrollBar.GetYCoordinateOfBufferPosition(changes[i].End)) + ChangePadding;
                }

                drawingContext.DrawRectangle(_brushes[(int)type], null,
                                             new Rect(ChangeOffset * 0.5, yTop, this.Width - ChangeOffset, yBottom - yTop));
            }
        }
    }
}
