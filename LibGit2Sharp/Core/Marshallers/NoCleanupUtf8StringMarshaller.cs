#nullable enable

namespace LibGit2Sharp.Core.Marshallers;

using System.Runtime.InteropServices.Marshalling;

[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedOut, typeof(NoCleanupUtf8StringMarshaller))]
internal static unsafe class NoCleanupUtf8StringMarshaller
{
    public static string? ConvertToManaged(byte* unmanaged) => Utf8StringMarshaller.ConvertToManaged(unmanaged);
}
