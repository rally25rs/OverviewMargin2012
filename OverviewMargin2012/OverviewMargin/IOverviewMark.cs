// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin
{
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Provides the information needed to render a tag in the overview margin.
    /// </summary>
    public interface IOverviewMark
    {
        /// <summary>
        /// Gets the color used to draw the mark in the overview margin.
        /// </summary>
        Color Color { get; }

        /// <summary>
        /// Gets the content to use when displaying a tooltip. 
        /// </summary>
        /// <remarks>
        /// The overview margin will add a line number; thus, the content
        /// returned by this method should not itself specify the location. 
        /// If this property is null, no tooltip will be displayed.
        /// </remarks>
        object ToolTipContent { get; }

        /// <summary>
        /// The possition in the buffer that corresponds to the location of the mark.
        /// </summary>
        /// <remarks>This can be a position in any buffer. The overview mark margin will handle the translation.</remarks>
        IMappingPoint Position { get; }
    }
}
