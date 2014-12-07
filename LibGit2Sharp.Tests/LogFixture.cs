using Xunit;

namespace LibGit2Sharp.Tests
{
    public class LogFixture
    {
        [Fact]
        public void CanEnableAndDisableLogging()
        {
            // Setting logging produces a log message at level Info,
            // ensure that we catch it.
            LogLevel level = LogLevel.None;
            string message = null;

            GlobalSettings.LogConfiguration = new LogConfiguration(LogLevel.Trace, (l, m) => { level = l; message = m; });

            Assert.Equal(LogLevel.Info, level);
            Assert.Equal("Logging enabled at level Trace", message);

            // Configuring at Warning and higher means that the
            // message at level Info should not be produced.
            level = LogLevel.None;
            message = null;

            GlobalSettings.LogConfiguration = new LogConfiguration(LogLevel.Warning, (l, m) => { level = l; message = m; });

            Assert.Equal(LogLevel.None, level);
            Assert.Equal(null, message);

            // Similarly, turning logging off should produce no messages.
            GlobalSettings.LogConfiguration = LogConfiguration.None;

            Assert.Equal(LogLevel.None, level);
            Assert.Equal(null, message);
        }
    }
}
