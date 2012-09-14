// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Names of common <see cref="ITextView"/> host-related options.
    /// </summary>
    public static class DefaultOverviewMarginOptions
    {
        #region Option identifiers

        /// <summary>
        /// Determines whether to have the right-hand change tracking margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> ChangeTrackingMarginId = new EditorOptionKey<bool>("OverviewMargin/ChangeTrackingMarginEnabled");

        /// <summary>
        /// Determines whether to have an overview mark margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> MarkMarginId = new EditorOptionKey<bool>("OverviewMargin/MarkMarginEnabled");

        /// <summary>
        /// Determines whether to have the overview margin include elided text.
        /// </summary>
        public static readonly EditorOptionKey<bool> ExpandElisionsInOverviewMarginId = new EditorOptionKey<bool>("OverviewMargin/ExpandElisionsInOverviewMargin");

        /// <summary>
        /// Determines size of the preview popup when the mouse enters the overview margin (0 == disabled)
        /// </summary>
        public static readonly EditorOptionKey<int> PreviewSizeId = new EditorOptionKey<int>("OverviewMargin/PreviewSize");

        #endregion
    }

    /// <summary>
    /// Defines the option to enable the right-hand change-tracking margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    public sealed class ChangeTrackingMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOverviewMarginOptions.ChangeTrackingMarginId; } }
    }

    /// <summary>
    /// Defines the option to enable the OverviewMark margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    public sealed class OverviewMarkMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOverviewMarginOptions.MarkMarginId; } }
    }

    /// <summary>
    /// Defines the option to show elided text in the overview margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    public sealed class ExpandElisionsInOverviewMargin : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOverviewMarginOptions.ExpandElisionsInOverviewMarginId; } }
    }

    /// <summary>
    /// Defines the option to show elided text in the overview margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    public sealed class PreviewSize : ViewOptionDefinition<int>
    {
        public override int Default { get { return 7; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return DefaultOverviewMarginOptions.PreviewSizeId; } }
    }
}
