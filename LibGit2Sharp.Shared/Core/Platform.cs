using System;
using System.IO;
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
        private static Lazy<OperatingSystemType> _operatingSystem = new Lazy<OperatingSystemType>(
            DetermineOperatingSystem,
            System.Threading.LazyThreadSafetyMode.PublicationOnly);

        public static string ProcessorArchitecture
        {
            get { return IntPtr.Size == 8 ? "x64" : "x86"; }
        }

        public static OperatingSystemType OperatingSystem => _operatingSystem.Value;

        private static OperatingSystemType DetermineOperatingSystem()
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
            try
            {
                return OperatingSystem_CoreFxStyle();
            }
            catch (FileNotFoundException)
            {
                // We're probably running on .NET 4.6.1 or earlier where the API isn't available.
                // This would suggest we're running on Windows. Although if our portable library
                // is being used on mono, it could be *nix or OSX too.
                return OperatingSystemType.Windows;
            }
#endif
        }

#if !NET40
        private static OperatingSystemType OperatingSystem_CoreFxStyle()
        {
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
        }
#endif
    }
}
