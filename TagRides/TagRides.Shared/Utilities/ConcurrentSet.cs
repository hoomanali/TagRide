using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace TagRides.Shared.Utilities
{
    /// <summary>
    /// A concurrent, simplified HashSet. Copied from http://source.roslyn.io/#microsoft.codeanalysis/InternalUtilities/ConcurrentSet.cs
    /// </summary>
    public sealed class ConcurrentSet<T> : ICollection<T>
    {
        /// <summary>
        /// The default concurrency level is 2. That means the collection can cope with up to two
        /// threads making simultaneous modifications without blocking.
        /// Note ConcurrentDictionary's default concurrency level is dynamic, scaling according to
        /// the number of processors.
        /// </summary>
        private const int DefaultConcurrencyLevel = 2;

        /// <summary>
        /// Taken from ConcurrentDictionary.DEFAULT_CAPACITY
        /// </summary>
        private const int DefaultCapacity = 31;

        /// <summary>
        /// The backing dictionary. The values are never used; just the keys.
        /// </summary>
        /// <remarks>
        /// This is marked volatile because it can be changed during
        /// <see cref="GetSnapshotAndClear"/>. The 'volatile' keyword ensures
        /// that on a multi-core processor, no CPU tries to add to the
        /// old dictionary after a new one is set.
        /// </remarks>
        private volatile ConcurrentDictionary<T, byte> _dictionary;

        /// <summary>
        /// Construct a concurrent set with the default concurrency level.
        /// </summary>
        public ConcurrentSet()
        {
            _dictionary = new ConcurrentDictionary<T, byte>(DefaultConcurrencyLevel, DefaultCapacity);
        }

        /// <summary>
        /// Gets a snapshot of the contents of the set.
        /// </summary>
        /// <returns>The contents of the set at the time of this method call.</returns>
        public ICollection<T> GetSnapshot()
        {
            return _dictionary.Keys;
        }

        /// <summary>
        /// Gets a snapshot of the contents of the set and removes all elements
        /// in the snapshot from the set.
        /// </summary>
        /// <returns>The snapshot of the contents of the set at the time of this method call.</returns>
        public ICollection<T> GetSnapshotAndClear()
        {
            var newDict = new ConcurrentDictionary<T, byte>(DefaultConcurrencyLevel, DefaultCapacity);

            // Swap in a new empty dictionary.
            var oldDict = Interlocked.Exchange(ref _dictionary, newDict);

            return oldDict.Keys;
        }

        /// <summary>
        /// Obtain the number of elements in the set.
        /// </summary>
        /// <returns>The number of elements in the set.</returns>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Determine whether the set is empty.</summary>
        /// <returns>true if the set is empty; otherwise, false.</returns>
        public bool IsEmpty => _dictionary.IsEmpty;

        public bool IsReadOnly => false;

        /// <summary>
        /// Determine whether the given value is in the set.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>true if the set contains the specified value; otherwise, false.</returns>
        public bool Contains(T value)
        {
            return _dictionary.ContainsKey(value);
        }

        /// <summary>
        /// Attempts to add a value to the set.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>true if the value was added to the set. If the value already exists, this method returns false.</returns>
        public bool TryAdd(T value)
        {
            return _dictionary.TryAdd(value, 0);
        }

        public void AddRange(IEnumerable<T> values)
        {
            if (values != null)
            {
                foreach (var v in values)
                {
                    TryAdd(v);
                }
            }
        }

        /// <summary>
        /// Attempts to remove a value from the set.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>true if the value was removed successfully; otherwise false.</returns>
        public bool TryRemove(T value)
        {
            return _dictionary.TryRemove(value, out _);
        }

        /// <summary>
        /// Clear the set
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        public struct KeyEnumerator
        {
            private readonly IEnumerator<KeyValuePair<T, byte>> _kvpEnumerator;

            internal KeyEnumerator(IEnumerable<KeyValuePair<T, byte>> data)
            {
                _kvpEnumerator = data.GetEnumerator();
            }

            public T Current => _kvpEnumerator.Current.Key;

            public bool MoveNext()
            {
                return _kvpEnumerator.MoveNext();
            }

            public void Reset()
            {
                _kvpEnumerator.Reset();
            }
        }

        /// <summary>
        /// Obtain an enumerator that iterates through the elements in the set.
        /// </summary>
        /// <returns>An enumerator for the set.</returns>
        public KeyEnumerator GetEnumerator()
        {
            // PERF: Do not use dictionary.Keys here because that creates a snapshot
            // of the collection resulting in a List<T> allocation. Instead, use the
            // KeyValuePair enumerator and pick off the Key part.
            return new KeyEnumerator(_dictionary);
        }

        private IEnumerator<T> GetEnumeratorImpl()
        {
            // PERF: Do not use dictionary.Keys here because that creates a snapshot
            // of the collection resulting in a List<T> allocation. Instead, use the
            // KeyValuePair enumerator and pick off the Key part.
            foreach (var kvp in _dictionary)
            {
                yield return kvp.Key;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        void ICollection<T>.Add(T item)
        {
            TryAdd(item);
        }

        bool ICollection<T>.Remove(T item)
        {
            return TryRemove(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
    }
}
