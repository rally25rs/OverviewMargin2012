// Copyright (c) Microsoft Corporation
// All rights reserved
namespace SyntacticFisheye
{
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Utilities;

    public class SyntacticFisheyeLineTransformSource : ILineTransformSource
    {
        #region private members
        private static LineTransform _defaultTransform = new LineTransform(0.0, 0.0, 1.0);  //No compression
        private static LineTransform _simpleTransform = new LineTransform(0.0, 0.0, 0.5);   //50% vertical compression
        #endregion

        /// <summary>
        /// Static class factory that ensures a single instance of the line transform source/view.
        /// </summary>
        public static SyntacticFisheyeLineTransformSource Create(IPropertyOwner view)
        {
            return view.Properties.GetOrCreateSingletonProperty<SyntacticFisheyeLineTransformSource>(delegate { return new SyntacticFisheyeLineTransformSource(); });
        }

        #region ILineTransformSource Members
        public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
        {
            //Compress lines that contain neither letters nor digits by 50%.
            for (int i = line.Start; (i < line.End); ++i)
            {
                if (char.IsLetterOrDigit(line.Snapshot[i]))
                    return _defaultTransform;
            }

            //Nothing found, so squish.
            return _simpleTransform;
        }
        #endregion
    }
}
