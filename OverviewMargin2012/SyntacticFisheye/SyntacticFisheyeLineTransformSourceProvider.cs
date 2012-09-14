// Copyright (c) Microsoft Corporation
// All rights reserved

namespace SyntacticFisheye
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// This class implements a connector that produces the SyntacticFisheye LineTransformSourceProvider.
    /// </summary>
    //[Export(typeof(ILineTransformSourceProvider))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class SyntacticFisheyeLineTransformSourceProvider : ILineTransformSourceProvider
    {
        public ILineTransformSource Create(IWpfTextView textView)
        {
            return SyntacticFisheyeLineTransformSource.Create(textView);
        }
    }
}

