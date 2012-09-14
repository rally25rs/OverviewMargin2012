// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.StructureAdornment
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Extensions.SettingsStore;

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class StructureAdornmentEnabled : ViewOptionDefinition<bool>
    {
        public override bool Default { get { return true; } }
        public override EditorOptionKey<bool> Key { get { return StructureAdornmentManager.EnabledOption; } }
    }

    [Export(typeof(IWpfTextViewCreationListener))]
    [Export(typeof(IMouseProcessorProvider))]
    [Name("StructureAdornment")]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class StructureAdornmentFactory : IWpfTextViewCreationListener, IMouseProcessorProvider
    {
        [Export]
        [Name("StructureAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        internal AdornmentLayerDefinition viewLayerDefinition;

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

        public void TextViewCreated(IWpfTextView textView)
        {
            StructureAdornmentManager.Create(textView, this);
        }

        public IMouseProcessor GetAssociatedProcessor(IWpfTextView textView)
        {
            return StructureAdornmentManager.Create(textView, this);
        }
    }
}
