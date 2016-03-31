using System;

namespace LibGit2Sharp
{
    internal interface ICustomMarshaler
    {
        Object MarshalNativeToManaged(IntPtr pNativeData);

        IntPtr MarshalManagedToNative(Object ManagedObj);

        void CleanUpNativeData(IntPtr pNativeData);

        void CleanUpManagedData(Object ManagedObj);

        int GetNativeDataSize();
    }
}
