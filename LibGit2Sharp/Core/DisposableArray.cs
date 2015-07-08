using System;
using System.Linq;
using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// An array containing disposables, for convenience when dealing with handles
    /// </summary>
    internal class DisposableArray<T> : IDisposable where T : IDisposable
    {
        readonly T[] array;

        /// <summary>
        /// Create a wrapper for the given array so the contents will be disposed when this class is disposed.
        /// </summary>
        /// <param name="handles">The array of dispsables</param>
        public DisposableArray(T[] handles)
        {
            array = handles;
        }

        /// <summary>
        /// Create a wrapper for the given array so the contents will be disposed when this class is disposed.
        /// <para>
        /// The enumerable is first made into an array
        /// </para>
        /// </summary>
        /// <param name="handles">Handles.</param>
        public DisposableArray(IEnumerable<T> handles)
        {
            array = handles.ToArray();
        }

        /// <summary>
        /// The underlying array
        /// </summary>
        public T[] Array { get { return array; } }

        /// <summary>
        /// Return the underlying array so we can use this wherever the methods expect an array
        /// </summary>
        public static implicit operator T[](DisposableArray<T> da)
        {
            return da.array;
        }

        /// <summary>
        /// Call Dispose on each of the elements of the array
        /// </summary>
        public void Dispose()
        {
            foreach (var handle in array)
            {
                handle.Dispose();
            }
        }
    }
}

