// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.StructureMargin
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Extensions.OverviewMargin;

    /// <summary>
    /// Implementation of a margin that show the structure of a code file.
    /// </summary>
    internal class StructureMargin : IWpfTextViewMargin, IOverviewTipFactory
    {
        /// <summary>
        /// Name of this margin.
        /// </summary>
        public const string Name = "Structure";

        #region Private Members
        StructureMarginElement structureMarginElement;
        bool isDisposed;
        #endregion

        /// <summary>
        /// Constructor for the StructureMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        public StructureMargin(IWpfTextViewHost textViewHost, IVerticalScrollBar scrollBar, StructureMarginFactory factory)
        {
            // Validate
            if (textViewHost == null)
                throw new ArgumentNullException("textViewHost");

            this.structureMarginElement = new StructureMarginElement(textViewHost.TextView, scrollBar, factory);
        }

        #region IWpfTextViewMargin Members
        /// <summary>
        /// The FrameworkElement that renders the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return this.structureMarginElement;
            }
        }
        #endregion

        #region ITextViewMargin Members
        /// <summary>
        /// For a horizontal margin, this is the height of the margin (since the width will be determined by the ITextView. For a vertical margin, this is the width of the margin (since the height will be determined by the ITextView.
        /// </summary>
        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return this.structureMarginElement.ActualWidth;
            }
        }

        /// <summary>
        /// The visible property, true if the margin is visible, false otherwise.
        /// </summary>
        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return this.structureMarginElement.Enabled;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, StructureMargin.Name, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                this.structureMarginElement.Dispose();
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }
        #endregion

        #region IOverviewTipFactory Members
        public bool UpdateTip(IOverviewMargin margin, MouseEventArgs e, ToolTip tip)
        {
            return this.structureMarginElement.UpdateTip(margin, e, tip);
        }
        #endregion

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(Name);
        }
    }
}
