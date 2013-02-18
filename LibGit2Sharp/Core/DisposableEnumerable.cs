using System;
using System.Collections;
using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
    internal class DisposableEnumerable<T> : IEnumerable<T>, IDisposable where T : IDisposable
    {
        private readonly IList<T> enumerable;

        public DisposableEnumerable(IList<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public int Count
        {
            get { return enumerable.Count; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            foreach (var entry in enumerable)
            {
                entry.Dispose();
            }
        }
    }
}
