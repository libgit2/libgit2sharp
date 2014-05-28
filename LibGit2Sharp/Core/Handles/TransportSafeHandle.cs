using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class TransportSafeHandle : NotOwnedSafeHandleBase
    {
        internal IntPtr DefinitionPtr { get; set; }
    }
}
