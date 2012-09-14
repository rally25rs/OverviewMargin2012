// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;
    using SettingsStore;

    [Export(typeof(IWpfTextViewMarginProvider))]
    [MarginContainer(PredefinedOverviewMarginNames.Overview)]
    [Name(PredefinedOverviewMarginNames.OverviewChangeTracking)]
    [Order(Before = PredefinedOverviewMarginNames.OverviewMark)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    sealed class OverviewChangeTrackingMarginProvider : IWpfTextViewMarginProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; private set; }

        [Import]
        internal IEditorFormatMapService EditorFormatMapService { get; private set; }

        [Import(AllowDefault = true)]
        internal ISettingsStore _settingsStore { get; set; }

        public bool LoadOption(IEditorOptions options, string optionName)
        {
            if (_settingsStore != null)
            {
                return _settingsStore.LoadOption(options, optionName);
            }
            return false;
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin parentMargin)
        {
            IOverviewMargin parent = parentMargin as IOverviewMargin;
            if (parent != null)
            {
                return OverviewChangeTrackingMargin.Create(
                    textViewHost,
                    parent.ScrollBar,
                    this);
            }
            else
                return null;
        }
    }
}
