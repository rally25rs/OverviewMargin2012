// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger.Implementation
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ITaggerProvider))]
    [ContentType("CSharp")]
    [TagType(typeof(IBlockTag))]
    internal class CsharpBlockTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(typeof(CsharpBlockTaggerProvider), delegate
            {
                return new GenericBlockTagger(buffer, new CsharpParser()) as ITagger<T>;
            });
        }
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("C/C++")]
    [TagType(typeof(IBlockTag))]
    internal class CppBlockTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(typeof(CppBlockTaggerProvider), delegate
            {
                return new GenericBlockTagger(buffer, new CppParser()) as ITagger<T>;
            });
        }
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("Basic")]
    [TagType(typeof(IBlockTag))]
    internal class VbBlockTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(typeof(VbBlockTaggerProvider), delegate
            {
                return new GenericBlockTagger(buffer, new VbParser()) as ITagger<T>;
            });
        }
    }
}