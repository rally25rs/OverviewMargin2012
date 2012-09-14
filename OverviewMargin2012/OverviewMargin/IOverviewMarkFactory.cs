// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Provides information describe the marks that should be drawn in the overview mark margin.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IOverviewMarkFactory))]
    /// [TextViewRole(...)]
    /// </remarks>
    public interface IOverviewMarkFactory
    {
        /// <summary>
        /// Gets the overview marks associated with a mapping span .
        /// </summary>
        /// <param name="span">The span for which to get the overview marks.</param>
        /// <returns>An enumeratation of <see cref="IOverviewMark"/>s that lie in <paramref name="span"/>.</returns>
        IEnumerable<IOverviewMark> GetOverviewMarks(IMappingSpan span);

        /// <summary>
        /// Raised by the factory when there is a change in the set of marks it provides
        /// </summary>
        event EventHandler<TagsChangedEventArgs> MarksChanged;
    }
}
