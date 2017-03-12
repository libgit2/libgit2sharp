using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal class ArrayMarshaler<T> : IDisposable
    {
        private readonly IntPtr[] ptrs;

        public ArrayMarshaler(T[] objs)
        {
            ptrs = new IntPtr[objs.Length];

            for (var i = 0; i < objs.Length; i++)
            {
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
                ptrs[i] = ptr;
                Marshal.StructureToPtr(objs[i], ptr, false);
            }
        }

        public int Count
        {
            get { return ptrs.Length; }
        }

        public IntPtr[] ToArray()
        {
            return ptrs;
        }

        public void Dispose()
        {
            foreach (var ptr in ptrs)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
