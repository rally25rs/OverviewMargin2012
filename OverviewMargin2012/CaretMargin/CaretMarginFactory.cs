// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.CaretMargin
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Extensions.OverviewMargin;
    using Microsoft.VisualStudio.Extensions.SettingsStore;

    [Export(typeof(IWpfTextViewMarginProvider))]
    [MarginContainer(PredefinedOverviewMarginNames.Overview)]
    [Name(CaretMargin.Name)]
    [Order(After = PredefinedOverviewMarginNames.OverviewMark, Before = "Structure")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    sealed class CaretMarginFactory : IWpfTextViewMarginProvider
    {
#pragma warning disable 649
        [Import(AllowDefault = true)]
        internal ISettingsStore _settingsStore { get; set; }
#pragma warning restore 649

        [Export]
        [Name("CaretAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Outlining, Before = PredefinedAdornmentLayers.Selection)]
        internal AdornmentLayerDefinition caretLayerDefinition;

        public bool LoadOption(IEditorOptions options, string optionName)
        {
            if (_settingsStore != null)
            {
                return _settingsStore.LoadOption(options, optionName);
            }
            return false;
        }

        /// <summary>
        /// Create an instance of the CaretMargin in the specified <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <param name="textViewHost">The <see cref="IWpfTextViewHost"/> in which the CaretMargin will be displayed.</param>
        /// <returns>The newly created CaretMargin.</returns>
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            IOverviewMargin containerMarginAsOverviewMargin = containerMargin as IOverviewMargin;
            if (containerMarginAsOverviewMargin != null)
            {
                //The caret margin needs to know what the constitutes a word, which means using the text structure navigator
                //(since the definition of a word can change based on context).

                //Create the caret margin, passing it a newly instantiated text structure navigator for the view.
                return new CaretMargin(textViewHost, containerMarginAsOverviewMargin.ScrollBar, this);
            }
            else
                return null;
        }
    }
}
