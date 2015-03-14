﻿using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Global settings for libgit2 and LibGit2Sharp.
    /// </summary>
    public static class GlobalSettings
    {
        private static readonly Lazy<Version> version = new Lazy<Version>(Version.Build);

        private static LogConfiguration logConfiguration = LogConfiguration.None;

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
                Proxy.git_transport_register(
                    registration.Scheme,
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
        /// Register a filter globally with a default priority of 200 allowing the custom filter 
        /// to imitate a core Git filter driver. It will be run last on checkout and first on checkin.
        /// </summary>
        public static FilterRegistration RegisterFilter(Filter filter)
        {
            return RegisterFilter(filter, 200);
        }
        /// <summary>
        /// Register a filter globally with given priority for execution.
        /// A filter with the priority of 200 will be run last on checkout and first on checkin.
        /// A filter with the priority of 0 will be run first on checkout and last on checkin.
        /// </summary>
        public static FilterRegistration RegisterFilter(Filter filter, int priority)
        {
            var registration = new FilterRegistration(filter);

            Proxy.git_filter_register(filter.Name, registration.FilterPointer, priority);

            return registration;
        }

        /// <summary>
        /// Remove the filter from the registry, and frees the native heap allocation.
        /// </summary>
        public static void DeregisterFilter(FilterRegistration registration)
        {
            Ensure.ArgumentNotNull(registration, "registration");

            Proxy.git_filter_unregister(registration.Name);
            registration.Free();
        }
    }
}
