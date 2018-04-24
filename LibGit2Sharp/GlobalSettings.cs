﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Global settings for libgit2 and LibGit2Sharp.
    /// </summary>
    public static class GlobalSettings
    {
        private static readonly Lazy<Version> version = new Lazy<Version>(Version.Build);
        private static readonly Dictionary<Filter, FilterRegistration> registeredFilters;

        private static LogConfiguration logConfiguration = LogConfiguration.None;

        private static string nativeLibraryPath;
        private static bool nativeLibraryPathLocked;
        private static string nativeLibraryDefaultPath;

        static GlobalSettings()
        {
            nativeLibraryDefaultPath = NativeLibraryPathResolver.GetNativeLibraryDefaultPath();

            registeredFilters = new Dictionary<Filter, FilterRegistration>();
        }

        /// <summary>
        /// Returns information related to the current LibGit2Sharp
        /// library.
        /// </summary>
        public static Version Version
        {
            get
            {
                return version.Value;
            }
        }

        /// <summary>
        /// Registers a new <see cref="SmartSubtransport"/> as a custom
        /// smart-protocol transport with libgit2.  Any Git remote with
        /// the scheme registered will delegate to the given transport
        /// for all communication with the server.  use this transport to communicate
        /// with the server This is not commonly
        /// used: some callers may want to re-use an existing connection to
        /// perform fetch / push operations to a remote.
        ///
        /// Note that this configuration is global to an entire process
        /// and does not honor application domains.
        /// </summary>
        /// <typeparam name="T">The type of SmartSubtransport to register</typeparam>
        /// <param name="scheme">The scheme (eg "http" or "gopher") to register</param>
        public static SmartSubtransportRegistration<T> RegisterSmartSubtransport<T>(string scheme)
            where T : SmartSubtransport, new()
        {
            Ensure.ArgumentNotNull(scheme, "scheme");

            var registration = new SmartSubtransportRegistration<T>(scheme);

            try
            {
                Proxy.git_transport_register(registration.Scheme,
                                             registration.FunctionPointer,
                                             registration.RegistrationPointer);
            }
            catch (Exception)
            {
                registration.Free();
                throw;
            }

            return registration;
        }

        /// <summary>
        /// Unregisters a previously registered <see cref="SmartSubtransport"/>
        /// as a custom smart-protocol transport with libgit2.
        /// </summary>
        /// <typeparam name="T">The type of SmartSubtransport to register</typeparam>
        /// <param name="registration">The previous registration</param>
        public static void UnregisterSmartSubtransport<T>(SmartSubtransportRegistration<T> registration)
            where T : SmartSubtransport, new()
        {
            Ensure.ArgumentNotNull(registration, "registration");

            Proxy.git_transport_unregister(registration.Scheme);
            registration.Free();
        }

        /// <summary>
        /// Registers a new <see cref="LogConfiguration"/> to receive
        /// information logging information from libgit2 and LibGit2Sharp.
        ///
        /// Note that this configuration is global to an entire process
        /// and does not honor application domains.
        /// </summary>
        public static LogConfiguration LogConfiguration
        {
            set
            {
                Ensure.ArgumentNotNull(value, "value");

                logConfiguration = value;

                if (logConfiguration.Level == LogLevel.None)
                {
                    Proxy.git_trace_set(0, null);
                }
                else
                {
                    Proxy.git_trace_set(value.Level, value.GitTraceCallback);

                    Log.Write(LogLevel.Info, "Logging enabled at level {0}", value.Level);
                }
            }

            get
            {
                return logConfiguration;
            }
        }

        /// <summary>
        /// Sets a path for loading native binaries on .NET Framework or .NET Core.
        /// When specified, native library will first be searched under the given path.
        /// On .NET Framework a subdirectory corresponding to the architecture  ("x86" or "x64") is appended,
        /// otherwise the native library is expected to be found in the directory as specified.
        ///
        /// If the library is not found it will be searched in standard search paths:
        /// <see cref="DllImportSearchPath.AssemblyDirectory"/>,
        /// <see cref="DllImportSearchPath.ApplicationDirectory"/> and
        /// <see cref="DllImportSearchPath.SafeDirectories"/>.
        /// <para>
        /// This must be set before any other calls to the library,
        /// and is not available on other platforms than .NET Framework and .NET Core.
        /// </para>
        /// <para>
        /// If not specified on .NET Framework it defaults to lib/win32 subdirectory
        /// of the directory where this assembly is loaded from.
        /// </para>
        /// </summary>
        public static string NativeLibraryPath
        {
            get
            {
                return nativeLibraryPath ?? nativeLibraryDefaultPath;
            }

            set
            {
                if (nativeLibraryPathLocked)
                {
                    throw new LibGit2SharpException("You cannot set the native library path after it has been loaded");
                }

                try
                {
                    nativeLibraryPath = Path.GetFullPath(value);
                }
                catch (Exception e)
                {
                    throw new LibGit2SharpException(e.Message);
                }
            }
        }

        internal static string GetAndLockNativeLibraryPath()
        {
            nativeLibraryPathLocked = true;

            return nativeLibraryPath ?? nativeLibraryDefaultPath;
        }

        /// <summary>
        /// Takes a snapshot of the currently registered filters.
        /// </summary>
        /// <returns>An array of <see cref="FilterRegistration"/>.</returns>
        public static IEnumerable<FilterRegistration> GetRegisteredFilters()
        {
            lock (registeredFilters)
            {
                FilterRegistration[] array = new FilterRegistration[registeredFilters.Count];
                registeredFilters.Values.CopyTo(array, 0);
                return array;
            }
        }

        /// <summary>
        /// Register a filter globally with a default priority of 200 allowing the custom filter
        /// to imitate a core Git filter driver. It will be run last on checkout and first on checkin.
        /// </summary>
        public static FilterRegistration RegisterFilter(Filter filter)
        {
            return RegisterFilter(filter, 200);
        }

        /// <summary>
        /// Registers a <see cref="Filter"/> to be invoked when <see cref="Filter.Name"/> matches .gitattributes 'filter=name'
        /// </summary>
        /// <param name="filter">The filter to be invoked at run time.</param>
        /// <param name="priority">The priroty of the filter to invoked.
        /// A value of 0 (<see cref="FilterRegistration.FilterPriorityMin"/>) will be run first on checkout and last on checkin.
        /// A value of 200 (<see cref="FilterRegistration.FilterPriorityMax"/>) will be run last on checkout and first on checkin.
        /// </param>
        /// <returns>A <see cref="FilterRegistration"/> object used to manage the lifetime of the registration.</returns>
        public static FilterRegistration RegisterFilter(Filter filter, int priority)
        {
            Ensure.ArgumentNotNull(filter, "filter");
            if (priority < FilterRegistration.FilterPriorityMin || priority > FilterRegistration.FilterPriorityMax)
            {
                throw new ArgumentOutOfRangeException("priority",
                                                      priority,
                                                      String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                    "Filter priorities must be within the inclusive range of [{0}, {1}].",
                                                                    FilterRegistration.FilterPriorityMin,
                                                                    FilterRegistration.FilterPriorityMax));
            }

            FilterRegistration registration = null;

            lock (registeredFilters)
            {
                // if the filter has already been registered
                if (registeredFilters.ContainsKey(filter))
                {
                    throw new EntryExistsException("The filter has already been registered.", GitErrorCode.Exists, GitErrorCategory.Filter);
                }

                // allocate the registration object
                registration = new FilterRegistration(filter, priority);
                // add the filter and registration object to the global tracking list
                registeredFilters.Add(filter, registration);
            }

            return registration;
        }

        /// <summary>
        /// Unregisters the associated filter.
        /// </summary>
        /// <param name="registration">Registration object with an associated filter.</param>
        public static void DeregisterFilter(FilterRegistration registration)
        {
            Ensure.ArgumentNotNull(registration, "registration");

            lock (registeredFilters)
            {
                var filter = registration.Filter;

                // do nothing if the filter isn't registered
                if (registeredFilters.ContainsKey(filter))
                {
                    // remove the register from the global tracking list
                    registeredFilters.Remove(filter);
                    // clean up native allocations
                    registration.Free();
                }
            }
        }

        internal static void DeregisterFilter(Filter filter)
        {
            System.Diagnostics.Debug.Assert(filter != null);

            // do nothing if the filter isn't registered
            if (registeredFilters.ContainsKey(filter))
            {
                var registration = registeredFilters[filter];
                // unregister the filter
                DeregisterFilter(registration);
            }
        }

        /// <summary>
        /// Get the paths under which libgit2 searches for the configuration file of a given level.
        /// </summary>
        /// <param name="level">The level (global/system/XDG) of the config.</param>
        /// <returns>The paths that are searched</returns>
        public static IEnumerable<string> GetConfigSearchPaths(ConfigurationLevel level)
        {
            return Proxy.git_libgit2_opts_get_search_path(level).Split(Path.PathSeparator);
        }

        /// <summary>
        /// Set the paths under which libgit2 searches for the configuration file of a given level.
        ///
        /// <seealso cref="RepositoryOptions"/>.
        /// </summary>
        /// <param name="level">The level (global/system/XDG) of the config.</param>
        /// <param name="paths">
        ///     The new search paths to set.
        ///     Pass null to reset to the default.
        ///     The special string "$PATH" will be substituted with the current search path.
        /// </param>
        public static void SetConfigSearchPaths(ConfigurationLevel level, params string[] paths)
        {
            var pathString = (paths == null) ? null : string.Join(Path.PathSeparator.ToString(), paths);
            Proxy.git_libgit2_opts_set_search_path(level, pathString);
        }

        public static void SetStrictHashVerification(bool enabled)
        {
            Proxy.git_libgit2_opts_enable_strict_hash_verification(enabled);
        }

        /// <summary>
        /// Enable or disable the libgit2 cache
        /// </summary>
        /// <param name="enabled">true to enable the cache, false otherwise</param>
        public static void SetEnableCaching(bool enabled)
        {
            Proxy.git_libgit2_opts_set_enable_caching(enabled);
        }

        /// <summary>
        /// Enable or disable the ofs_delta capability
        /// </summary>
        /// <param name="enabled">true to enable the ofs_delta capability, false otherwise</param>
        public static void SetEnableOfsDelta(bool enabled)
        {
            Proxy.git_libgit2_opts_set_enable_ofsdelta(enabled);
        }

        /// <summary>
        /// Enable or disable the libgit2 strict_object_creation capability
        /// </summary>
        /// <param name="enabled">true to enable the strict_object_creation capability, false otherwise</param>
        public static void SetEnableStrictObjectCreation(bool enabled)
        {
            Proxy.git_libgit2_opts_set_enable_strictobjectcreation(enabled);
        }
    }
}
