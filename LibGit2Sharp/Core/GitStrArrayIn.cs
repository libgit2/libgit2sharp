using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitStrArrayIn : IDisposable
    {
        public IntPtr strings;
        public uint size;

        public static GitStrArrayIn BuildFrom(FilePath[] paths)
        {
            var nbOfPaths = paths.Length;
            var pathPtrs = new IntPtr[nbOfPaths];

            for (int i = 0; i < nbOfPaths; i++)
            {
                var s = paths[i].Posix;
                pathPtrs[i] = FilePathMarshaler.FromManaged(s);
            }

            int dim = IntPtr.Size * nbOfPaths;

            IntPtr arrayPtr = Marshal.AllocHGlobal(dim);
            Marshal.Copy(pathPtrs, 0, arrayPtr, nbOfPaths);

            return new GitStrArrayIn { strings = arrayPtr, size = (uint)nbOfPaths };
        }

        public void Dispose()
        {
            if (size == 0)
            {
                return;
            }

            var nbOfPaths = (int)size;

            var pathPtrs = new IntPtr[nbOfPaths];
            Marshal.Copy(strings, pathPtrs, 0, nbOfPaths);

            for (int i = 0; i < nbOfPaths; i++)
            {
                Marshal.FreeHGlobal(pathPtrs[i]);
            }

            Marshal.FreeHGlobal(strings);
            size = 0;
        }
    }
}
