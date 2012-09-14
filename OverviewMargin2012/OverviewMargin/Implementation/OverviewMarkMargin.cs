// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Implementation of an IWpfTextViewMargin that shows all changes in the file.
    /// </summary>
    sealed class OverviewMarkMargin : IWpfTextViewMargin, IOverviewTipFactory
    {
        #region Private Members
        internal MarkMarginElement _markMarginElement;
        private bool _isDisposed = false;

        /// <summary>
        /// Constructor for the OverviewChangeTrackingMargin.
        /// </summary>
        private OverviewMarkMargin(
            IWpfTextViewHost textViewHost,
            IVerticalScrollBar scrollBar,
            OverviewMarkMarginProvider provider)
        {
            _markMarginElement = new MarkMarginElement(textViewHost.TextView, scrollBar, provider);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("OverviewMarkMargin");
        }
        #endregion

        /// <summary>
        /// Factory for the OverviewMarkMargin.
        /// </summary>
        public static OverviewMarkMargin Create(
            IWpfTextViewHost textViewHost,
            IVerticalScrollBar scrollBar,
            OverviewMarkMarginProvider provider)
        {
            // Validate
            if (textViewHost == null)
                throw new ArgumentNullException("textViewHost");

            return new OverviewMarkMargin(textViewHost, scrollBar, provider);
        }

        #region IWpfTextViewMargin Members
        /// <summary>
        /// The FrameworkElement that renders the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            get
            {
                this.ThrowIfDisposed();
                return _markMarginElement;
            }
        }
        #endregion

        #region ITextViewMargin Members
        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();
                return _markMarginElement.ActualWidth;
            }
        }

        public bool Enabled
        {
            get
            {
                this.ThrowIfDisposed();
                return _markMarginElement.Enabled;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, PredefinedOverviewMarginNames.OverviewMark, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _markMarginElement.Dispose();
                _isDisposed = true;
            }
        }
        #endregion

        #region IOverviewTipFactory Members
        public bool UpdateTip(IOverviewMargin margin, MouseEventArgs e, System.Windows.Controls.ToolTip tip)
        {
            return _markMarginElement.UpdateTip(margin, e, tip);
        }
        #endregion
    }
}
