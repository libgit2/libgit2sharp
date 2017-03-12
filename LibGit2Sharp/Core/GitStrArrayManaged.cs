using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A git_strarray where the string array and strings themselves were allocated
    /// with LibGit2Sharp's allocator (Marshal.AllocHGlobal).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitStrArrayManaged : IDisposable
    {
        public GitStrArray Array;

        public static GitStrArrayManaged BuildFrom(string[] strings)
        {
            return BuildFrom(strings, StrictUtf8Marshaler.FromManaged);
        }

        public static GitStrArrayManaged BuildFrom(FilePath[] paths)
        {
            return BuildFrom(paths, StrictFilePathMarshaler.FromManaged);
        }

        private static GitStrArrayManaged BuildFrom<T>(T[] input, Func<T, IntPtr> marshaler)
        {
            var pointers = new IntPtr[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                var item = input[i];
                pointers[i] = marshaler(item);
            }

            var toReturn = new GitStrArrayManaged();

            toReturn.Array.Strings = Marshal.AllocHGlobal(checked(IntPtr.Size * input.Length));
            Marshal.Copy(pointers, 0, toReturn.Array.Strings, input.Length);
            toReturn.Array.Count = new UIntPtr((uint)input.Length);

            return toReturn;
        }

        public void Dispose()
        {
            var count = checked((int)Array.Count.ToUInt32());

            for (int i = 0; i < count; i++)
            {
                EncodingMarshaler.Cleanup(Marshal.ReadIntPtr(Array.Strings, i * IntPtr.Size));
            }

            if (Array.Strings != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Array.Strings);
            }

            // Now that we've freed the memory, zero out the structure.
            Array.Reset();
        }
    }
}
