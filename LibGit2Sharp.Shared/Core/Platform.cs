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
        public static string ProcessorArchitecture
        {
            get { return IntPtr.Size == 8 ? "x64" : "x86"; }
        }

        public static OperatingSystemType OperatingSystem
        {
            get
            {
#if NET40
                // See http://www.mono-project.com/docs/faq/technical/#how-to-detect-the-execution-platform
                switch ((int)Environment.OSVersion.Platform)
                {
                    case 4:
                    case 128:
                        return OperatingSystemType.Unix;

                    case 6:
                        return OperatingSystemType.MacOSX;

                    default:
                        return OperatingSystemType.Windows;
                }
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OperatingSystemType.Windows;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OperatingSystemType.Unix;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OperatingSystemType.MacOSX;
                }
                else
                {
                    throw new InvalidOperationException();
                }
#endif
            }
        }
    }
}
