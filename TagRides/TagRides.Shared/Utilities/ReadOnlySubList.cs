using System;
using System.Collections;
using System.Collections.Generic;

namespace TagRides.Shared.Utilities
{
    /// <summary>
    /// An efficient view into a readonly list. Stores just the reference to the
    /// original list and two indices. Should only be used with lists that do not
    /// change length.
    /// </summary>
    public class ReadOnlySubList<T> : IReadOnlyList<T>
    {
        public T this[int index]
        {
            get
            {
                if (index >= endIdx)
                    throw new IndexOutOfRangeException();

                return superList[startIdx + index];
            }
        }

        public int Count => endIdx - startIdx;

        public ReadOnlySubList(ReadOnlySubList<T> superList)
            : this(superList, 0, superList.Count)
        {
        }

        public ReadOnlySubList(IReadOnlyList<T> superList)
            : this(superList, 0, superList.Count)
        {
        }

        public ReadOnlySubList(ReadOnlySubList<T> superList, int first, int count)
        {
            // Point directly to original list.
            this.superList = superList.superList;
            startIdx = superList.startIdx + first;
            endIdx = superList.startIdx + first + count;
        }

        public ReadOnlySubList(IReadOnlyList<T> superList, int first, int count)
        {
            this.superList = superList;
            startIdx = first;
            endIdx = startIdx + count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < endIdx - startIdx; ++i)
                yield return superList[startIdx + i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IReadOnlyList<T> superList;
        readonly int startIdx;
        readonly int endIdx;
    }
}
