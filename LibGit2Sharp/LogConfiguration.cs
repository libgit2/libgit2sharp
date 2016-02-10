using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Logging and tracing configuration for libgit2 and LibGit2Sharp.
    /// </summary>
    public sealed class LogConfiguration
    {
        /// <summary>
        /// The default logging configuration, which performs no logging at all.
        /// </summary>
        public static readonly LogConfiguration None = new LogConfiguration { Level = LogLevel.None };

        /// <summary>
        /// Creates a new logging configuration to call the given
        /// delegate when logging occurs at the given level.
        /// </summary>
        /// <param name="level">Level to log at</param>
        /// <param name="handler">Handler to call when logging occurs</param>
        public LogConfiguration(LogLevel level, LogHandler handler)
        {
            Ensure.ArgumentConformsTo<LogLevel>(level, (t) => { return (level != LogLevel.None); }, "level");
            Ensure.ArgumentNotNull(handler, "handler");

            Level = level;
            Handler = handler;

            // Explicitly create (and hold a reference to) a callback-delegate to wrap GitTraceHandler().
            GitTraceCallback = GitTraceHandler;
        }

        private LogConfiguration()
        { }

        internal LogLevel Level { get; private set; }
        internal LogHandler Handler { get; private set; }
        internal NativeMethods.git_trace_cb GitTraceCallback { get; private set; }

        /// <summary>
        /// This private method will be called from LibGit2 (from C code via
        /// the GitTraceCallback delegate) to route LibGit2 log messages to
        /// the same LogHandler as LibGit2Sharp messages.
        /// </summary>
        private void GitTraceHandler(LogLevel level, IntPtr msg)
        {
            string message = LaxUtf8Marshaler.FromNative(msg);
            Handler(level, message);
        }
    }
}
