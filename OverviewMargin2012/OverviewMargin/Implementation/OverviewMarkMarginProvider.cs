// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
    using SettingsStore;

    [Export(typeof(IWpfTextViewMarginProvider))]
    [MarginContainer(PredefinedOverviewMarginNames.Overview)]
    [Name(PredefinedOverviewMarginNames.OverviewMark)]
    [Order(After = PredefinedOverviewMarginNames.OverviewChangeTracking)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    sealed class OverviewMarkMarginProvider : IWpfTextViewMarginProvider
    {
        [Import]
        internal IEditorFormatMapService EditorFormatMapService { get; private set; }

        [ImportMany]
        internal List<Lazy<IOverviewMarkFactoryProvider, ITextViewRoleMetadata>> MarkProviders { get; private set; }

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
                return OverviewMarkMargin.Create(
                    textViewHost,
                    parent.ScrollBar,
                    this);
            }
            else
                return null;
        }
    }

    [Export(typeof(IOverviewTipFactoryProvider))]
    [Name(PredefinedOverviewMarginNames.OverviewMark)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    sealed class OverviewMarkMarginTipProvider : IOverviewTipFactoryProvider
    {
        public IOverviewTipFactory GetOverviewTipFactory(IOverviewMargin overviewMargin, IWpfTextView view)
        {
            ITextViewMargin margin = overviewMargin as ITextViewMargin;

            return margin != null ? (margin.GetTextViewMargin(PredefinedOverviewMarginNames.OverviewMark) as IOverviewTipFactory) : null;
        }
    }
}
