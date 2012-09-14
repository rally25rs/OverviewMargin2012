// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.CaretMargin
{
    using System;
    using System.Windows;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Implementation of an IWpfTextViewMargin that highlights the location of the caret
    /// and all instances of words that match the word under the caret.
    /// </summary>
    internal class CaretMargin : IWpfTextViewMargin
    {
        /// <summary>
        /// Name of this margin.
        /// </summary>
        public const string Name = "Caret";

        #region Private Members
        CaretMarginElement caretMarginElement;
        bool _isDisposed = false;
        #endregion

        /// <summary>
        /// Constructor for the CaretMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        /// <param name="navigator">Instance of an ITextStructureNavigator used to define words in the host's TextView. Created from the
        /// ITextStructureNavigatorFactory service.</param>
        public CaretMargin(IWpfTextViewHost textViewHost, IVerticalScrollBar scrollBar, CaretMarginFactory factory)
        {
            // Validate
            if (textViewHost == null)
                throw new ArgumentNullException("textViewHost");

            this.caretMarginElement = new CaretMarginElement(textViewHost.TextView, factory, scrollBar);
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
                return this.caretMarginElement;
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
                return this.caretMarginElement.ActualWidth;
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
                return this.caretMarginElement.Enabled;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, CaretMargin.Name, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        /// <summary>
        /// In our dipose, stop listening for events.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                this.caretMarginElement.Dispose();
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
        #endregion

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(Name);
        }

    }
}
