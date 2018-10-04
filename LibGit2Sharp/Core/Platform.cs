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
#if NETFRAMEWORK 
        private static bool? _isRunningOnMac;
        private static bool IsRunningOnMac() => _isRunningOnMac ?? (_isRunningOnMac = TryGetIsRunningOnMac()) ?? false;
#endif

        public static OperatingSystemType OperatingSystem
        {
            get
            {
#if NETFRAMEWORK
                var platform = (int)Environment.OSVersion.Platform;
                if (platform <= 3 || platform == 5)
                {
                    return OperatingSystemType.Windows;
                }
                if (IsRunningOnMac())
                {
                    return OperatingSystemType.MacOSX;
                }
                if (platform == 4 || platform == 6 || platform == 128)
                {
                    return OperatingSystemType.Unix;
                }
#else
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
#endif
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

#if NETFRAMEWORK
#pragma warning disable IDE1006 // Naming Styles
        [DllImport("libc")]
        private static extern int sysctlbyname(
            [MarshalAs(UnmanagedType.LPStr)] string property,
            IntPtr output,
            IntPtr oldLen,
            IntPtr newp,
            uint newlen);
#pragma warning restore IDE1006 // Naming Styles

        private static bool TryGetIsRunningOnMac()
        {
            const string OsType = "kern.ostype";
            const string MacOsType = "Darwin";

            return MacOsType == GetOsType();

            string GetOsType()
            {
                try
                {
                    IntPtr
                        pointerLength = IntPtr.Zero,
                        pointerString = IntPtr.Zero;

                    try
                    {
                        pointerLength = Marshal.AllocHGlobal(sizeof(int));

                        sysctlbyname(OsType, IntPtr.Zero, pointerLength, IntPtr.Zero, 0);

                        var length = Marshal.ReadInt32(pointerLength);

                        if (length <= 0)
                        {
                            return string.Empty;
                        }

                        pointerString = Marshal.AllocHGlobal(length);

                        sysctlbyname(OsType, pointerString, pointerLength, IntPtr.Zero, 0);

                        return Marshal.PtrToStringAnsi(pointerString);
                    }
                    finally
                    {
                        if (pointerLength != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(pointerLength);
                        }
                        if (pointerString != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(pointerString);
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
#endif
    }
}
