namespace LibGit2Sharp
{
    internal class Log
    {
        private static bool IsEnabled(LogConfiguration configuration, LogLevel level)
        {
            return (configuration.Level != LogLevel.None && configuration.Level >= level);
        }

        internal static bool IsEnabled(LogLevel level)
        {
            return IsEnabled(GlobalSettings.LogConfiguration, level);
        }

        internal static void Write(LogLevel level, string message, params object[] args)
        {
            LogConfiguration configuration = GlobalSettings.LogConfiguration;

            if (!IsEnabled(configuration, level))
            {
                return;
            }

            configuration.Handler(level, string.Format(message, args));
        }
    }
}
