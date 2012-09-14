// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Represents an overview margin.
    /// </summary>
    public interface IOverviewMargin
    {
        /// <summary>
        /// Gets the <see cref="IVerticalScrollBar"/> used to map between buffer positions and y-coordinates.
        /// </summary>
        IVerticalScrollBar ScrollBar
        {
            get;
        }
    }
}
