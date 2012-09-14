// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger.Implementation
{
    using Microsoft.VisualStudio.Text;

    class BaseFilter
    {
        protected ITextSnapshot snapshot;
        protected int position;

        public BaseFilter(ITextSnapshot snapshot)
        {
            this.snapshot = snapshot;
            this.position = -1;
        }

        public char Character { get { return this.snapshot[this.position]; } }
        public int Position { get { return this.position; } }

        protected char PeekNextChar()
        {
            return PeekNextChar(1);
        }

        protected char PeekNextChar(int offset)
        {
            if (this.position < this.snapshot.Length - offset)
                return this.snapshot[this.Position + offset];
            else
                return ' ';
        }
    }
}
