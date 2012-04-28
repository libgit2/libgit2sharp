using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal class FilePathMarshaler : Utf8Marshaler
    {
        private static readonly FilePathMarshaler staticInstance = new FilePathMarshaler();

        public override IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
            {
                return IntPtr.Zero;
            }

            if (!(managedObj is FilePath))
            {
                throw new MarshalDirectiveException("FilePathMarshaler must be used on a FilePath.");
            }

            return StringToNative(((FilePath)managedObj).Posix);
        }

        public override object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return (FilePath)NativeToString(pNativeData);
        }

        public new static ICustomMarshaler GetInstance(string cookie)
        {
            return staticInstance;
        }
    }
}
