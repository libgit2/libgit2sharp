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

                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Determines the RID to use when loading libgit2 native library.
        /// This method only supports RIDs that are currently used by LibGit2Sharp.NativeBinaries.
        /// </summary>
        public static string GetNativeLibraryRuntimeId()
        {
            switch (OperatingSystem)
            {
                case OperatingSystemType.MacOSX:
                    return "osx";

                case OperatingSystemType.Unix:
                    return "linux-" + ProcessorArchitecture;

                case OperatingSystemType.Windows:
                    return "win7-" + ProcessorArchitecture;
            }

            throw new PlatformNotSupportedException();
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
            => Type.GetType("Mono.Runtime") != null;

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
