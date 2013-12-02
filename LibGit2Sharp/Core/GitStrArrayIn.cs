using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitStrArrayIn : IDisposable
    {
        public IntPtr strings;
        public uint size;

        public static GitStrArrayIn BuildFrom(string[] strings)
        {
            return BuildFrom(strings, StrictUtf8Marshaler.FromManaged);
        }

        public static GitStrArrayIn BuildFrom(FilePath[] paths)
        {
            return BuildFrom(paths, StrictFilePathMarshaler.FromManaged);
        }

        private static GitStrArrayIn BuildFrom<T>(T[] input, Func<T, IntPtr> marshaler)
        {
            var count = input.Length;
            var pointers = new IntPtr[count];

            for (int i = 0; i < count; i++)
            {
                var item = input[i];
                pointers[i] = marshaler(item);
            }

            int dim = IntPtr.Size * count;

            IntPtr arrayPtr = Marshal.AllocHGlobal(dim);
            Marshal.Copy(pointers, 0, arrayPtr, count);

            return new GitStrArrayIn { strings = arrayPtr, size = (uint)count };
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
