// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides an <see cref="IOverviewTipFactory"/>.
    /// </summary>
    /// <remarks>Implementors of this interface are MEF component parts, and should be exported with the following attributes:
    /// <code>
    /// [Export(typeof(IOverviewTipFactoryProvider))]
    /// [TextViewRole(...)]
    /// [ContentType(...)]
    /// [Name(...)]
    /// [Order(After = ..., Before = ...)]
    /// </code>
    /// </remarks>
    public interface IOverviewTipFactoryProvider
    {
        /// <summary>
        /// Gets the <see cref="IOverviewTipFactory"/> for the given text view and margin.
        /// </summary>
        /// <param name="margin">The margin for which the factory is being created.</param>
        /// <param name="view">The view for which the factory is being created.</param>
        /// <returns>An <see cref="IOverviewTipFactory"/> for the given view.</returns>
        IOverviewTipFactory GetOverviewTipFactory(IOverviewMargin margin, IWpfTextView view);
    }
}