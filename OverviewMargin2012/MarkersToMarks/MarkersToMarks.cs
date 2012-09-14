// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.MarkersToMarks
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Runtime.InteropServices;
    using System.Windows.Media;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Extensions.OverviewMargin;

    [Export(typeof(IOverviewMarkFactoryProvider))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class MarkFactoryProvider : IOverviewMarkFactoryProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService _aggregatorFactoryService = null;

        public IOverviewMarkFactory GetOverviewMarkFactory(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<MarkFactory>(delegate { return new MarkFactory(view, _aggregatorFactoryService); });
        }

        private sealed class MarkFactory : IOverviewMarkFactory
        {
            internal ITagAggregator<IVsVisibleTextMarkerTag> _markerAggregator = null;

            public MarkFactory(IWpfTextView view, IViewTagAggregatorFactoryService aggregatorFactoryService)
            {
                _markerAggregator = aggregatorFactoryService.CreateTagAggregator<IVsVisibleTextMarkerTag>(view);

                _markerAggregator.TagsChanged += OnTagsChanged;

                view.Closed += OnClosed;
            }

            void OnClosed(object sender, EventArgs e)
            {
                _markerAggregator.TagsChanged -= OnTagsChanged;
                _markerAggregator.Dispose();
            }

            void OnTagsChanged(object sender, TagsChangedEventArgs e)
            {
                EventHandler<TagsChangedEventArgs> handler = this.MarksChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }

            public IEnumerable<IOverviewMark> GetOverviewMarks(IMappingSpan span)
            {
                foreach (var tag in _markerAggregator.GetTags(span))
                {
                    Mark mark = Mark.Create(tag);
                    if (mark != null)
                        yield return mark;
                }
            }

            public event EventHandler<TagsChangedEventArgs> MarksChanged;

            private sealed class Mark : IOverviewMark
            {
                private Color? _color;
                private IMappingPoint _position;
                private IVsVisibleTextMarkerTag _tag;
                private string _hoverTip;

                public static Mark Create(IMappingTagSpan<IVsVisibleTextMarkerTag> tag)
                {
                    uint flags;
                    int hr = tag.Tag.StreamMarker.GetVisualStyle(out flags);
                    if (ErrorHandler.Succeeded(hr) &&
                        ((flags & (uint)MARKERVISUAL.MV_GLYPH) != 0) &&
                        ((flags & ((uint)MARKERVISUAL.MV_COLOR_ALWAYS | (uint)MARKERVISUAL.MV_COLOR_LINE_IF_NO_MARGIN)) != 0))
                    {
                        return new Mark(tag);
                    }

                    return null;
                }

                private Mark(IMappingTagSpan<IVsVisibleTextMarkerTag> tag)
                {
                    _tag = tag.Tag;
                    _position = tag.Span.Start;
                }

                public Color Color
                {
                    get
                    {
                        if (!_color.HasValue)
                        {
                            COLORINDEX[] foreground = new COLORINDEX[1];
                            COLORINDEX[] background = new COLORINDEX[1];
                            int hr = _tag.MarkerType.GetDefaultColors(foreground, background);
                            if (ErrorHandler.Succeeded(hr))
                            {
                                _color = Common.GetColorFromIndex(background[0]);
                            }
                            else
                            {
                                _color = Colors.Black;
                            }
                        }

                        return _color.Value;
                    }
                }

                public IMappingPoint Position
                {
                    get { return _position; }
                }

                public object ToolTipContent
                {
                    get
                    {
                        if (_hoverTip == null)
                        {
                            string[] tip = new string[1];
                            int hr = _tag.StreamMarker.GetTipText(tip);
                            if (ErrorHandler.Succeeded(hr) && (tip[0] != null))
                                _hoverTip = tip[0];
                            else
                                _hoverTip = string.Empty;
                        }

                        return _hoverTip.Length != 0 ? _hoverTip : null;
                    }
                }
            }
        }

        public static class Common
        {
            private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider _globalServiceProvider;
            private static IVsFontAndColorUtilities _fontAndColorUtilities;

            internal static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider
            {
                get
                {
                    if (_globalServiceProvider == null)
                        _globalServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)(Package.GetGlobalService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider)));

                    return _globalServiceProvider;
                }
                set // Exposed for unit testing.
                {
                    _globalServiceProvider = value;
                }
            }

            internal static InterfaceType GetService<InterfaceType, ServiceType>(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
            {
                return (InterfaceType)GetService(serviceProvider, typeof(ServiceType).GUID, false);
            }

            internal static object GetService(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider, Guid guidService, bool unique)
            {
                Guid guidInterface = VSConstants.IID_IUnknown;
                IntPtr ptrObject = IntPtr.Zero;
                object service = null;

                int hr = serviceProvider.QueryService(ref guidService, ref guidInterface, out ptrObject);
                if (hr >= 0 && ptrObject != IntPtr.Zero)
                {
                    try
                    {
                        if (unique)
                        {
                            service = Marshal.GetUniqueObjectForIUnknown(ptrObject);
                        }
                        else
                        {
                            service = Marshal.GetObjectForIUnknown(ptrObject);
                        }
                    }
                    finally
                    {
                        Marshal.Release(ptrObject);
                    }
                }

                return service;
            }

            internal static IVsFontAndColorUtilities FontAndColorUtilities
            {
                get
                {
                    if (_fontAndColorUtilities == null)
                        _fontAndColorUtilities = Common.GetService<IVsFontAndColorUtilities, SVsFontAndColorStorage>(Common.GlobalServiceProvider);

                    return _fontAndColorUtilities;
                }
                set // Exposed for unit testing.
                {
                    _fontAndColorUtilities = value;
                }
            }

            public static Color GetColorFromIndex(COLORINDEX color)
            {
                UInt32 w32color;
                Marshal.ThrowExceptionForHR(Common.FontAndColorUtilities.GetRGBOfIndex(color, out w32color));

                System.Drawing.Color gdiColor = System.Drawing.ColorTranslator.FromWin32((int)w32color);

                return Color.FromArgb(gdiColor.A, gdiColor.R, gdiColor.G, gdiColor.B);
            }
        }
    }
}
