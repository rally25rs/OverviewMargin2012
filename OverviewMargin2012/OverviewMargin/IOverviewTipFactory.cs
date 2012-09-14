// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin
{
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Provides information about what <see cref="ToolTip"/> should be shown when the mouse hovers over the overview margin.
    /// </summary>

    public interface IOverviewTipFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="margin">The <see cref="IOverviewMargin"/> for which the tip should be displayed.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> that cause the tip to be displayed.</param>
        /// <param name="tip"></param>
        /// <returns>True if this factory has modified the tip.</returns>
        bool UpdateTip(IOverviewMargin margin, MouseEventArgs e, ToolTip tip);
    }
}
