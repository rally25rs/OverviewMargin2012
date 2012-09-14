// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System;
    using System.Windows;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Implementation of an IWpfTextViewMargin that shows all changes in the file.
    /// </summary>
    sealed class OverviewChangeTrackingMargin : IWpfTextViewMargin
    {
        #region Private Members
        internal ChangeTrackingMarginElement _changeTrackingMarginElement;
        internal bool _isDisposed = false;

        /// <summary>
        /// Constructor for the OverviewChangeTrackingMargin.
        /// </summary>
        private OverviewChangeTrackingMargin(IWpfTextViewHost textViewHost, IVerticalScrollBar scrollBar, OverviewChangeTrackingMarginProvider provider)
        {
            _changeTrackingMarginElement = new ChangeTrackingMarginElement(textViewHost.TextView, scrollBar, provider);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("OverviewChangeTrackingMargin");
        }
        #endregion

        /// <summary>
        /// Factory for the ChangeTrackingMargin.
        /// </summary>
        public static OverviewChangeTrackingMargin Create(IWpfTextViewHost textViewHost, IVerticalScrollBar scrollBar, OverviewChangeTrackingMarginProvider provider)
        {
            // Validate
            if (textViewHost == null)
                throw new ArgumentNullException("textViewHost");

            return new OverviewChangeTrackingMargin(textViewHost, scrollBar, provider);
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
                return _changeTrackingMarginElement;
            }
        }
        #endregion

        #region ITextViewMargin Members
        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();
                return _changeTrackingMarginElement.ActualWidth;
            }
        }

        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return _changeTrackingMarginElement.Enabled;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, PredefinedOverviewMarginNames.OverviewChangeTracking, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _changeTrackingMarginElement.Dispose();
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
        #endregion
    }
}
