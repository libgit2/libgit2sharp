using System;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_person
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;

        [MarshalAs(UnmanagedType.LPStr)]
        public string email;

        public ulong time;

        internal Person Build()
        {
            return new Person(name, email, EpochHelper.ToDateTimeOffset((int)time));
        }
    }
}