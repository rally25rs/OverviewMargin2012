// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.VisualStudio.Utilities;

    public interface IContentTypeMetadata
    {
        IEnumerable<string> ContentTypes { get; }
    }

    public interface ITextViewRoleMetadata
    {
        IEnumerable<string> TextViewRoles { get; }
    }

    public interface ITipMetadata : IOrderable, IContentTypeMetadata, ITextViewRoleMetadata
    {
    }

    public interface IWpfTextViewMarginMetadata : IOrderable, IContentTypeMetadata, ITextViewRoleMetadata
    {
        /// <summary>
        /// Gets the name of the margin that contains this margin.
        /// </summary>
        string MarginContainer { get; }

        /// <summary>
        /// Gets the grid unit type to be used for drawing of this element in the container margin's grid.
        /// </summary>
        [DefaultValue(GridUnitType.Auto)]
        GridUnitType GridUnitType { get; }

        /// <summary>
        /// Gets the size of the grid cell in which the margin should be placed.
        /// </summary>
        [DefaultValue(1.0)]
        double GridCellLength { get; }
    }
}
