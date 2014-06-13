using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitStrArrayOut : IDisposable
    {
        public IntPtr strings;
        public uint size;

        public IEnumerable<string> Build()
        {
            int count = (int)size;
            var pointers = new IntPtr[count];

            Marshal.Copy(strings, pointers, 0, count);

            for (int i = 0; i < count; i++)
            {
                yield return LaxUtf8Marshaler.FromNative(pointers[i]);
            }
        }

        public void Dispose()
        {
            if (size == 0)
            {
                return;
            }

            var count = (int)size;

            var pointers = new IntPtr[count];
            Marshal.Copy(strings, pointers, 0, count);

            for (int i = 0; i < count; i++)
            {
                EncodingMarshaler.Cleanup(pointers[i]);
            }

            Marshal.FreeHGlobal(strings);
            size = 0;
        }
    }
}
