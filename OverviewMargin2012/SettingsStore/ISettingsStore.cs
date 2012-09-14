// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.SettingsStore
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    /// <summary>
    /// Provides an <see cref="ISettingsStore"/>, which can be used to persist options between sessions.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ISettingsStore))]
    /// </remarks>
    public interface ISettingsStore
    {
        /// <summary>
        /// Load the option from whatever persistent store is being used.
        /// </summary>
        /// <returns>true if the option was loaded successfully.</returns>
        bool LoadOption(IEditorOptions options, string optionName);

        /// <summary>
        /// Save the option to whatever persistent store is being used.
        /// </summary>
        /// <returns>true if the option was saved successfully.</returns>
        bool SaveOption(IEditorOptions options, string optionName);
    }
}
