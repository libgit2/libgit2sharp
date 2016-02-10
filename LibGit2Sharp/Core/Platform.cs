using System;

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
            get { return Is64BitProcess ? "x64" : "x86"; }
        }

        private static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }

        public static OperatingSystemType OperatingSystem
        {
            get
            {
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
            }
        }
    }
}
