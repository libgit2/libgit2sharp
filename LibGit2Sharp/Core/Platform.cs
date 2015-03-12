using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Core
{
    internal enum OperatingSystemType
    {
        Windows,
        Unix,
        MacOSX
    }

    internal class Platform
    {
        public static string ProcessorArchitecture
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return "amd64";
                }

                return "x86";
            }
        }

        public static OperatingSystemType OperatingSystem
        {
            get
            {
                // See http://www.mono-project.com/docs/faq/technical/#how-to-detect-the-execution-platform
                var platformId = (int)Environment.OSVersion.Platform;

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
