using System;

namespace LibGit2Sharp.Core
{
    internal enum OperatingSystemType
    {
        Windows,
        Unix,
        MacOSX
    }
    internal enum Architecture
    {
        x86,
        amd64
    }


    internal static class Platform
    {
        public static Architecture ProcessorArchitecture
        {
            get { return Is64BitProcess ? Architecture.amd64 : Architecture.x86; }
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
