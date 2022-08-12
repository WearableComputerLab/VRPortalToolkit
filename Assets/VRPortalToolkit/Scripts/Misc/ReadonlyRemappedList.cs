using System;
using System.Collections;
using System.Collections.Generic;

namespace Misc
{
    public class ReadonlyRemappedList<TOriginal, TData> : IReadOnlyList<TData>
    {
        /// <summary>
        /// Enumerates an element <see cref="IList{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TData>
        {
            private readonly IList<TOriginal> list;
            private readonly Func<TOriginal, TData> remapper;
            private readonly int start;
            private readonly int count;
            private int index;

            /// <inheritdoc />
            public TData Current { get; private set; }

            /// <inheritdoc />
            object IEnumerator.Current {
                get {
                    if (index == start || index == count + 1)
                    {
                        throw new InvalidOperationException();
                    }

                    return Current;
                }
            }

            /// <summary>
            /// Creates a new <see cref="Enumerator"/> that can enumerate the given <see cref="IList{T}"/>.
            /// </summary>
            /// <param name="list">The list to enumerate.</param>
            /// <param name="start">The index to start enumerating at.</param>
            /// <param name="count">How many items to enumerate over.</param>
            public Enumerator(IList<TOriginal> list, Func<TOriginal, TData> remapper, int start, int count)
            {
                this.list = list ?? Array.Empty<TOriginal>();
                this.start = start;
                this.count = count;
                this.remapper = remapper ?? ((_) => default(TData));
                index = start;
                Current = default;
            }

            /// <inheritdoc />
            public void Dispose() { }

            /// <inheritdoc />
            public bool MoveNext()
            {
                if (index < count)
                {
                    Current = remapper(list[index]);
                    index++;
                    return true;
                }

                index = count + 1;
                Current = default;
                return false;
            }

            /// <inheritdoc />
            public void Reset()
            {
                index = start;
                Current = default;
            }
        }

        private readonly IList<TOriginal> list;
        private readonly Func<TOriginal, TData> remapper;
        private readonly int start;
        private readonly int count;

        /// <inheritdoc/>
        public int Count => list?.Count ?? 0;
        /// <inheritdoc/>
        public TData this[int index] => remapper((list ?? Array.Empty<TOriginal>())[index]);

        /// <summary>
        /// Creates a new instance of <see cref="HeapAllocationFreeReadOnlyList{T}"/>.
        /// </summary>
        /// <param name="list">The list to enumerate.</param>
        /// <param name="start">The index to start enumerating at.</param>
        /// <param name="count">How many items to enumerate over.</param>
        public ReadonlyRemappedList(IList<TOriginal> list, Func<TOriginal, TData> remapper, int start, int count)
        {
            this.list = list;
            this.start = start;
            this.count = count;
            this.remapper = remapper ?? ((_) => default(TData));
        }

        /// <summary>
        /// Returns an enumerator that iterates through the elements.
        /// </summary>
        /// <returns>An enumerator to iterate through the elements.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(list, remapper, start, count);
        }

        /// <inheritdoc/>
        IEnumerator<TData> IEnumerable<TData>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}