// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    public class GenericBlockTagger : ITagger<IBlockTag>
    {
        #region private
        private class BackgroundScan
        {
            private bool abort = false;
            public delegate void CompletionCallback(CodeBlock root);

            /// <summary>
            /// Does a background scan in <paramref name="snapshot"/>. Call
            /// <paramref name="completionCallback"/> once the scan has completed.
            /// </summary>
            /// <param name="snapshot">Text snapshot in which to scan.</param>
            /// <param name="completionCallback">Delegate to call if the scan is completed (will be called on the UI thread).</param>
            /// <remarks>The constructor must be called from the UI thread.</remarks>
            public BackgroundScan(ITextSnapshot snapshot, IParser parser, CompletionCallback completionCallback)
            {
                ThreadPool.QueueUserWorkItem(delegate(object state)
                                        {
                                            //Lower our priority so that we do not compete with the rendering.
                                            System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                                            System.Threading.Thread.CurrentThread.IsBackground = true;

                                            CodeBlock newRoot = parser.Parse(snapshot, delegate { return this.abort; });

                                            if ((newRoot != null) && !this.abort)
                                                completionCallback(newRoot);
                                        });
            }

            /// <summary>
            /// About the current scan.
            /// </summary>
            public void Abort()
            {
                this.abort = true;
            }
        }

        private ITextBuffer buffer;
        private IParser parser;
        private BackgroundScan scan;
        private CodeBlock root;

        void OnChanged(object sender, TextContentChangedEventArgs e)
        {
            if (AnyTextChanges(e.Before.Version, e.After.Version))
                this.ScanBuffer(e.After);
        }

        private static bool AnyTextChanges(ITextVersion oldVersion, ITextVersion currentVersion)
        {
            while (oldVersion != currentVersion)
            {
                if (oldVersion.Changes.Count > 0)
                    return true;
                oldVersion = oldVersion.Next;
            }

            return false;
        }

        private void ScanBuffer(ITextSnapshot snapshot)
        {
            if (this.scan != null)
            {
                //Stop and blow away the old scan (even if it didn't finish, the results are not interesting anymore).
                this.scan.Abort();
                this.scan = null;
            }

            //The underlying buffer could be very large, meaning that doing the scan for all matches on the UI thread
            //is a bad idea. Do the scan on the background thread and use a callback to raise the changed event when
            //the entire scan has completed.
            this.scan = new BackgroundScan(snapshot, this.parser,
                                            delegate(CodeBlock newRoot)
                                            {
                                                //This delegate is executed on a background thread.
                                                this.root = newRoot;

                                                EventHandler<SnapshotSpanEventArgs> handler = _tagsChanged;
                                                if (handler != null)
                                                    handler(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
                                            });
        }
        #endregion

        public GenericBlockTagger(ITextBuffer buffer, IParser parser)
        {
            this.buffer = buffer;
            this.parser = parser;
        }

        public IEnumerable<ITagSpan<IBlockTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            CodeBlock root = this.root;  //this.root could be set on a background thread, so get a snapshot.
            if (root != null)
            {
                if (root.Span.Snapshot != spans[0].Snapshot)
                {
                    //There is a version skew between when the parse was done and what is being asked for.
                    IList<SnapshotSpan> translatedSpans = new List<SnapshotSpan>(spans.Count);
                    foreach (var span in spans)
                        translatedSpans.Add(span.TranslateTo(root.Span.Snapshot, SpanTrackingMode.EdgeInclusive));

                    spans = new NormalizedSnapshotSpanCollection(translatedSpans);
                }

                foreach (var child in root.Children)
                {
                    foreach (var tag in GetTags(child, spans))
                        yield return tag;
                }
            }
        }

        private static IEnumerable<ITagSpan<IBlockTag>> GetTags(CodeBlock block, NormalizedSnapshotSpanCollection spans)
        {
            if (spans.IntersectsWith(new NormalizedSnapshotSpanCollection(block.Span)))
            {
                yield return new TagSpan<IBlockTag>(block.Span, block);

                foreach (var child in block.Children)
                {
                    foreach (var tag in GetTags(child, spans))
                        yield return tag;
                }
            }
        }

        private EventHandler<SnapshotSpanEventArgs> _tagsChanged;
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add
            {
                lock(this)
                {
                    EventHandler<SnapshotSpanEventArgs> original = _tagsChanged;
                    _tagsChanged += value;

                    if (original == null)
                    {
                        this.buffer.Changed += OnChanged;
                        this.ScanBuffer(this.buffer.CurrentSnapshot);
                    }
                }
            }

            remove
            {
                lock(this)
                {
                    _tagsChanged -= value;
                    if (_tagsChanged == null)
                    {
                        this.buffer.Changed -= OnChanged;

                        if (this.scan != null)
                        {
                            //Stop and blow away the old scan (even if it didn't finish, the results are not interesting anymore).
                            this.scan.Abort();
                            this.scan = null;
                        }
                        this.root = null; //Allow the old root to be GC'd
                    }
                }
            }
        }
    }
}
