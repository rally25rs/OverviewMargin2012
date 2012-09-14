// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.SettingsStore.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Text.Editor;
    using System.Windows.Media;
    using System.Globalization;

    /// <summary>
    /// Provides an implementation of an ISettingsStore that saves options in the registry.
    /// </summary>
    [Export(typeof(ISettingsStore))]
    internal sealed class SettingsStoreImpl : ISettingsStore
    {
        [Import(typeof(Microsoft.VisualStudio.Shell.SVsServiceProvider))]
        IServiceProvider _serviceProvider
        {
            get;
            set;
        }

        private T GetService<T>(Type serviceType) where T : class
        {
            return _serviceProvider.GetService(serviceType) as T;
        }

        private IVsSettingsManager SettingsManagerService
        {
            get
            {
                return this.GetService<IVsSettingsManager>(typeof(SVsSettingsManager));
            }
        }

        private static EditorOptionDefinition GetMatchingOption(IEditorOptions options, string optionName)
        {
            foreach (var supportedOption in options.SupportedOptions)
            {
                if (supportedOption.Name == optionName)
                    return supportedOption;
            }

            return null;
        }

        public bool LoadOption(IEditorOptions options, string optionName)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            EditorOptionDefinition optionDefinition = null;
            while (true)
            {
                optionDefinition = GetMatchingOption(options, optionName);
                if (optionDefinition != null)
                    break;

                options = options.Parent;
                if (options == null)
                    return false;       //Unable to load option.
            }

            IVsSettingsManager manager = this.SettingsManagerService;
            IVsSettingsStore store;
            Marshal.ThrowExceptionForHR(manager.GetReadOnlySettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out store));

            string result;
            Marshal.ThrowExceptionForHR(store.GetStringOrDefault("Text Editor", optionName, string.Empty, out result));

            if (result == string.Empty)
            {
                //No corresponding entry in the registery. Save the option to make it finable and editable.
                this.SaveOption(options, optionName);
            }
            else
            {
                try
                {
                    if (optionDefinition.ValueType == typeof(bool))
                    {
                        options.SetOptionValue(optionName, bool.Parse(result));
                        return true;
                    }
                    else if (optionDefinition.ValueType == typeof(int))
                    {
                        options.SetOptionValue(optionName, int.Parse(result));
                        return true;
                    }
                    else if (optionDefinition.ValueType == typeof(double))
                    {
                        options.SetOptionValue(optionName, double.Parse(result));
                        return true;
                    }
                    else if (optionDefinition.ValueType == typeof(string))
                    {
                        options.SetOptionValue(optionName, result);
                        return true;
                    }
                    else if (optionDefinition.ValueType == typeof(Color))
                    {
                        //Color's saved by Color.ToString() have a leading # sign ... strip that off before we parse.
                        uint argb = uint.Parse(result.Substring(1), NumberStyles.AllowHexSpecifier);
                        options.SetOptionValue(optionName, Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
                        return true;
                    }
                }
                catch (System.FormatException)
                {
                    //If we get a format exception, then the data for the option is invalid: overwrite it with something in the correct format.
                    this.SaveOption(options, optionName);
                }
            }

            return false;
        }

        public bool SaveOption(IEditorOptions options, string optionName)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            while (true)
            {
                if (options.IsOptionDefined(optionName, true))
                {
                    break;
                }

                options = options.Parent;
                if (options == null)
                    return false;       //Unable to save option.
            }

            IVsSettingsManager manager = this.SettingsManagerService;
            IVsWritableSettingsStore store;
            Marshal.ThrowExceptionForHR(manager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out store));

            string result = options.GetOptionValue(optionName).ToString();

            Marshal.ThrowExceptionForHR(store.SetString("Text Editor", optionName, result));

            return true;
        }
    }
}