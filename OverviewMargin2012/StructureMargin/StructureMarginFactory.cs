// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.StructureMargin
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Extensions.OverviewMargin;
    using Microsoft.VisualStudio.Extensions.SettingsStore;

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(StructureMargin.Name)]
    [Order(After = "Caret")]
    [Order(After = PredefinedOverviewMarginNames.OverviewMark)]
    [MarginContainer(PredefinedOverviewMarginNames.Overview)]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class StructureMarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; set; }
        
        [Import(AllowDefault = true)]
        internal ISettingsStore SettingsStore { get; set; }

        public bool LoadOption(IEditorOptions options, string optionName)
        {
            if (this.SettingsStore != null)
            {
                return this.SettingsStore.LoadOption(options, optionName);
            }
            return false;
        }

        /// <summary>
        /// Create an instance of the StructureMargin in the specified <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <param name="textViewHost">The <see cref="IWpfTextViewHost"/> in which the StructureMargin will be displayed.</param>
        /// <param name="parentMargin">The scrollBar used to translate between buffer positions and y-coordinates.</param>
        /// <returns>The newly created StructureMargin.</returns>
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            IOverviewMargin containerMarginAsOverviewMargin = containerMargin as IOverviewMargin;
            if (containerMarginAsOverviewMargin != null)
            {
                //Create the caret margin, passing it a newly instantiated text structure navigator for the view.
                return new StructureMargin(textViewHost, containerMarginAsOverviewMargin.ScrollBar, this);
            }
            else
                return null;
        }
    }

    [Export(typeof(IOverviewTipFactoryProvider))]
    [Name(StructureMargin.Name)]
    [ContentType("text")]
    [Order(After = PredefinedOverviewMarginNames.OverviewMark)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class StructureTipFactory : IOverviewTipFactoryProvider
    {
        public IOverviewTipFactory GetOverviewTipFactory(IOverviewMargin overviewMargin, IWpfTextView view)
        {
            ITextViewMargin margin = overviewMargin as ITextViewMargin;

            return margin != null ? (margin.GetTextViewMargin(StructureMargin.Name) as IOverviewTipFactory) : null;
        }
    }
}
