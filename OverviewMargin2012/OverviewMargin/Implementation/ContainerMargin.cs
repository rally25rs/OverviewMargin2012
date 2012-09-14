// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.OverviewMargin.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A base class implementation of an editor margin that hosts other editor margins inside
    /// it. The control can be oriented either horizontally or vertically and supports ordered
    /// margins to be added inside it.
    /// </summary>
    internal class ContainerMargin : Grid, IWpfTextViewMargin
    {
        #region Private Members
        bool _isDiposed = false;
        bool _ignoreChildVisibilityEvents = false;
        internal List<Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>> _currentMargins;

        private readonly string _marginName;
        private readonly Orientation _orientation;
        private readonly IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> _marginProviders;

        protected readonly IWpfTextViewHost TextViewHost;

        protected ContainerMargin(string name, Orientation orientation, IWpfTextViewHost textViewHost,
                                  IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> marginProviders)
        {
            _marginName = name;
            _orientation = orientation;

            this.TextViewHost = textViewHost;
            _marginProviders = new List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>>();

            var viewRoles = this.TextViewHost.TextView.Roles;

            foreach (var marginProvider in marginProviders)
            {
                if (String.Compare(marginProvider.Metadata.MarginContainer, _marginName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (viewRoles.ContainsAny(marginProvider.Metadata.TextViewRoles))
                    {
                        _marginProviders.Add(marginProvider);
                    }
                }
            }
        }
        #endregion

        #region IWpfTextViewMargin Members
        /// <summary>
        /// The FrameworkElement that renders the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            get
            {
                this.ThrowIfDisposed();
                return this;
            }
        }
        #endregion

        #region ITextViewMargin Members
        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();

                return (_orientation == Orientation.Horizontal)
                       ? this.ActualHeight
                       : this.ActualWidth;
            }
        }

        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            if (string.Compare(marginName, _marginName, StringComparison.OrdinalIgnoreCase) == 0)
                return this;
            else
            {
                foreach (var marginData in _currentMargins)
                {
                    ITextViewMargin result = marginData.Item2.GetTextViewMargin(marginName);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public void Dispose()
        {
            if (!_isDiposed)
            {
                this.Close();
                GC.SuppressFinalize(this);
                _isDiposed = true;
            }
        }
        #endregion

        protected void ThrowIfDisposed()
        {
            if (_isDiposed)
                throw new ObjectDisposedException("ContainerMarginMargin");
        }

        protected virtual void Initialize()
        {
            this.TextViewHost.TextView.TextDataModel.ContentTypeChanged += OnContentTypeChanged;

            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    this.RegisterEvents();
                }
                else
                {
                    this.UnregisterEvents();
                }
            };

            this.AddMargins(this.GetMarginProviders(), null);
        }

        protected virtual void RegisterEvents()
        {
        }

        protected virtual void UnregisterEvents()
        {
        }

        protected virtual void AddMargins(IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> providers,
                                          List<Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>> oldMargins)
        {
            _currentMargins = new List<Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>>(providers.Count);

            try
            {
                _ignoreChildVisibilityEvents = true;

                // reset to a clean state before adding margins
                this.RowDefinitions.Clear();
                this.ColumnDefinitions.Clear();
                this.Children.Clear();

                foreach (var marginProvider in providers)
                {
                    //Try and re-use the existing margin if possible ... see if marginProvider exists in the old margins
                    var marginData = (oldMargins == null)
                                     ? null
                                     : oldMargins.Find(delegate(Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin> a)
                                     {
                                         return (marginProvider == a.Item1);
                                     });

                    //And re-use the margin if it exists. Create a new one if it doesn't.
                    IWpfTextViewMargin margin = (marginData != null) ? marginData.Item2 : marginProvider.Value.CreateMargin(this.TextViewHost, this);
                    if (margin != null)
                        this.AddMargin(margin, marginProvider, marginData == null);
                }
            }
            finally
            {
                _ignoreChildVisibilityEvents = false;
            }

            // check to see if any visible children are available
            this.Visibility = this.HasVisibleChild() ? Visibility.Visible : Visibility.Collapsed;
        }

        protected virtual void Close()
        {
            this.TextViewHost.TextView.TextDataModel.ContentTypeChanged -= OnContentTypeChanged;

            foreach (var margin in _currentMargins)
            {
                this.DisposeMargin(margin.Item2);
            }
            _currentMargins.Clear();
        }

        private IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> GetMarginProviders()
        {
            IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> providers = new List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>>(_marginProviders.Count);

            foreach (var marginProvider in _marginProviders)
            {
                foreach (string contentType in marginProvider.Metadata.ContentTypes)
                {
                    if (this.TextViewHost.TextView.TextDataModel.ContentType.IsOfType(contentType))
                    {
                        providers.Add(marginProvider);
                        break;
                    }
                }
            }

            return providers;
        }

        private void AddMargin(IWpfTextViewMargin margin, Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata> marginProvider, bool track)
        {
            _currentMargins.Add(new Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>(marginProvider, margin));

            // calculate the length of the grid cell used to hold the margin
            GridLength gridCellLength = new GridLength(marginProvider.Metadata.GridCellLength, marginProvider.Metadata.GridUnitType);
            if (_orientation == Orientation.Horizontal)
            {
                RowDefinition newRow = new RowDefinition();
                newRow.Height = gridCellLength;
                this.RowDefinitions.Add(newRow);

                Grid.SetColumn(margin.VisualElement, 0);
                Grid.SetRow(margin.VisualElement, this.RowDefinitions.Count - 1);
            }
            else
            {
                ColumnDefinition newColumn = new ColumnDefinition();
                newColumn.Width = gridCellLength;
                this.ColumnDefinitions.Add(newColumn);

                Grid.SetColumn(margin.VisualElement, this.ColumnDefinitions.Count - 1);
                Grid.SetRow(margin.VisualElement, 0);
            }

            this.Children.Add(margin.VisualElement);

            if (track)
            {
                DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
                if (descriptor != null)
                    descriptor.AddValueChanged(margin.VisualElement, OnChildMarginVisibilityChanged);
            }
        }

        private void DisposeMargin(IWpfTextViewMargin margin)
        {
            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
            if (descriptor != null)
                descriptor.RemoveValueChanged(margin.VisualElement, OnChildMarginVisibilityChanged);

            margin.Dispose();
        }

        void OnChildMarginVisibilityChanged(object sender, EventArgs e)
        {
            if (!_ignoreChildVisibilityEvents)
            {
                if (this.Visibility == System.Windows.Visibility.Collapsed)
                {
                    // show the container margin if it's collapsed right now and one of its children became visible
                    if (this.HasVisibleChild())
                        this.Visibility = Visibility.Visible;
                }
                else if (this.Visibility == System.Windows.Visibility.Visible)
                {
                    // if the container margin is visible and all of its children are collapsed then collapse
                    // the container
                    if (!this.HasVisibleChild())
                        this.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private bool HasVisibleChild()
        {
            foreach (var export in _currentMargins)
            {
                if (export.Item2.VisualElement.Visibility != System.Windows.Visibility.Collapsed)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e)
        {
            //Go through and generate a new list of margin providers
            IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> providers = this.GetMarginProviders();

            //Dispose of any margin in _currentMargins that isn't listed in the new providers.
            for (int i = _currentMargins.Count - 1; (i >= 0); --i)
            {
                var marginData = _currentMargins[i];
                if (!providers.Contains(marginData.Item1))
                {
                    this.DisposeMargin(marginData.Item2);
                    _currentMargins.RemoveAt(i);
                }
            }

            this.AddMargins(providers, _currentMargins);
        }
    }
}
