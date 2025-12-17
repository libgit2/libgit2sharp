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
        public static string ProcessorArchitecture => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

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

                throw new PlatformNotSupportedException();
            }
        }

        public static string GetNativeLibraryExtension()
        {
            switch (OperatingSystem)
            {
                case OperatingSystemType.MacOSX:
                    return ".dylib";

                case OperatingSystemType.Unix:
                    return ".so";

                case OperatingSystemType.Windows:
                    return ".dll";
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Returns true if the runtime is Mono.
        /// </summary>
        public static bool IsRunningOnMono()
#if NETFRAMEWORK
            => Type.GetType("Mono.Runtime") != null;
#else
            => false;
#endif

        /// <summary>
        /// Returns true if the runtime is .NET Framework.
        /// </summary>
        public static bool IsRunningOnNetFramework()
            => typeof(object).Assembly.GetName().Name == "mscorlib" && !IsRunningOnMono();

        /// <summary>
        /// Returns true if the runtime is .NET Core.
        /// </summary>
        public static bool IsRunningOnNetCore()
            => typeof(object).Assembly.GetName().Name != "mscorlib";
    }
}
