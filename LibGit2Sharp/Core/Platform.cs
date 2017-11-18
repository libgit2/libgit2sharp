using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal enum OperatingSystemType
    {
        Windows,
        Unix,
        MacOSX
    }

    internal static class Platform
    {
        public static string ProcessorArchitecture => IntPtr.Size == 8 ? "x64" : "x86";

        public static OperatingSystemType OperatingSystem
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OperatingSystemType.Windows;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OperatingSystemType.Unix;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OperatingSystemType.MacOSX;
                }

                throw new InvalidOperationException();
            }
        }
    }
}
