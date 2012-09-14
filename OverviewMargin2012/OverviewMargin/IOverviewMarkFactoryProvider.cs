// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin
{

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides an <see cref="IOverviewMarkFactory"/>.
    /// </summary>
    /// <remarks>Implementors of this interface are MEF component parts, and should be exported with the following attribute:
    /// <code>
    /// [Export(typeof(IOverviewMarkFactoryProvider))]
    /// Exporters must supply at least one TagTypeAttribute.
    /// </code>
    /// </remarks>
    public interface IOverviewMarkFactoryProvider
    {
        /// <summary>
        /// Gets the <see cref="IOverviewMarkFactory"/> for the given text view and margin.
        /// </summary>
        /// <param name="view">The view for which the factory is being created.</param>
        /// <returns>An <see cref="IOverviewMarkFactory"/> for the given view.</returns>
        IOverviewMarkFactory GetOverviewMarkFactory(IWpfTextView view);
    }
}