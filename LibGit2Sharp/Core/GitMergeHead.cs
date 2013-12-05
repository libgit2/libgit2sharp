using LibGit2Sharp.Core.Handles;
using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitMergeHead
    {
	    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string RefName;
	    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string RemoteUrl;

	    GitOid Oid;
	    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string OidStr;
	    GitObjectSafeHandle Commit;
    }
}
