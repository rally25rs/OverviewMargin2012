// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.ErrorsToMarks
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Extensions.OverviewMargin;

    [Export(typeof(IOverviewMarkFactoryProvider))]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    class ErrorMarkFactoryProvider : IOverviewMarkFactoryProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; set; }

        [Import]
        internal IEditorFormatMapService EditorFormatMapService { get; set; }

        public IOverviewMarkFactory GetOverviewMarkFactory(IWpfTextView view)
        {
            return new ErrorMarkFactory(view, this.TagAggregatorFactoryService, this.EditorFormatMapService);
        }
    }

    class ErrorMarkFactory : IOverviewMarkFactory
    {
        private IWpfTextView _view;
        private ITagAggregator<IErrorTag> _squiggleTagger;
        private IEditorFormatMap _editorFormatMap;
        private Dictionary<string, Color> _cachedColors = new Dictionary<string, Color>();

        public ErrorMarkFactory(IWpfTextView view, IViewTagAggregatorFactoryService tagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService)
        {
            _squiggleTagger = tagAggregatorFactoryService.CreateTagAggregator<IErrorTag>(view);
            _squiggleTagger.TagsChanged += OnTagsChanged;

            _editorFormatMap = editorFormatMapService.GetEditorFormatMap(view);
            _editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;

            _view = view;
            _view.Closed += OnClosed;
        }

        void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            _cachedColors.Clear();

            EventHandler<TagsChangedEventArgs> handler = this.MarksChanged;
            if (handler != null)
            {
                handler(sender, new TagsChangedEventArgs(_view.BufferGraph.CreateMappingSpan(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length), SpanTrackingMode.EdgeInclusive)));
            }
        }

        void OnTagsChanged(object sender, TagsChangedEventArgs e)
        {
            EventHandler<TagsChangedEventArgs> handler = this.MarksChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (_squiggleTagger != null)
            {
                _squiggleTagger.TagsChanged -= OnTagsChanged;
                _squiggleTagger.Dispose();

                _editorFormatMap.FormatMappingChanged -= OnFormatMappingChanged;

                _view.Closed -= OnClosed;

                _squiggleTagger = null;
            }
        }

        private Color GetColor(IErrorTag tag)
        {
            string type = tag.ErrorType;
            Color color;
            if (!_cachedColors.TryGetValue(type, out color))
            {
                ResourceDictionary resourceDictionary = _editorFormatMap.GetProperties(type);

                if (resourceDictionary.Contains(EditorFormatDefinition.ForegroundColorId))
                {
                    color = (Color)resourceDictionary[EditorFormatDefinition.ForegroundColorId];
                }
                else
                {
                    color = Colors.Red;
                }

                _cachedColors.Add(type, color);
            }

            return color;
        }

        public IEnumerable<IOverviewMark> GetOverviewMarks(IMappingSpan span)
        {
            foreach (var tagSpan in _squiggleTagger.GetTags(span))
            {
                yield return new ErrorMark(tagSpan.Span.Start, tagSpan.Tag.ToolTipContent, GetColor(tagSpan.Tag));
            }
        }

        public event EventHandler<TagsChangedEventArgs> MarksChanged;
    }

    class ErrorMark : IOverviewMark
    {
        IMappingPoint _position;
        object _content;
        Color _color;

        public ErrorMark(IMappingPoint position, object content, Color color)
        {
            _position = position;
            _content = content;
            _color = color;
        }

        public Color Color
        {
            get { return _color; }
        }

        public object ToolTipContent
        {
            get { return _content; }
        }

        public IMappingPoint Position
        {
            get { return _position; }
        }
    }
}
