using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using LibGit2Sharp.Core;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class Constants
    {
        public static readonly string TemporaryReposPath = BuildPath();
        public const string UnknownSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        public static readonly Identity Identity = new Identity("A. U. Thor", "thor@valhalla.asgard.com");
        public static readonly Identity Identity2 = new Identity("nulltoken", "emeric.fermas@gmail.com");

        public static readonly Signature Signature = new Signature(Identity, new DateTimeOffset(2011, 06, 16, 10, 58, 27, TimeSpan.FromHours(2)));
        public static readonly Signature Signature2 = new Signature(Identity2, DateTimeOffset.Parse("Wed, Dec 14 2011 08:29:03 +0100"));

        // Populate these to turn on live credential tests:  set the
        // PrivateRepoUrl to the URL of a repository that requires
        // authentication. Define PrivateRepoCredentials to return an instance of
        // UsernamePasswordCredentials (for HTTP Basic authentication) or
        // DefaultCredentials (for NTLM/Negotiate authentication).
        //
        // For example:
        // public const string PrivateRepoUrl = "https://github.com/username/PrivateRepo";
        // ... return new UsernamePasswordCredentials { Username = "username", Password = "swordfish" };
        //
        // Or:
        // ... return new SecureUsernamePasswordCredentials() { Username = "username", Password = StringToSecureString("swordfish") };
        //
        // Or:
        // public const string PrivateRepoUrl = "https://tfs.contoso.com/tfs/DefaultCollection/project/_git/project";
        // ... return new DefaultCredentials();

        public const string PrivateRepoUrl = "";

        public static bool IsRunningOnUnix
        {
            get
            {
                return Platform.OperatingSystem == OperatingSystemType.MacOSX ||
                       Platform.OperatingSystem == OperatingSystemType.Unix;
            }
        }

        public static Credentials PrivateRepoCredentials(string url, string usernameFromUrl,
                                                         SupportedCredentialTypes types)
        {
            return null;
        }

        public static string BuildPath()
        {
            string tempPath = null;

            const string LibGit2TestPath = "LibGit2TestPath";

            // We're running on .Net/Windows
            if (Environment.GetEnvironmentVariables().Contains(LibGit2TestPath))
            {
                Trace.TraceInformation("{0} environment variable detected", LibGit2TestPath);
                tempPath = Environment.GetEnvironmentVariables()[LibGit2TestPath] as string;
            }

            if (string.IsNullOrWhiteSpace(tempPath) || !Directory.Exists(tempPath))
            {
                Trace.TraceInformation("Using default test path value");
                tempPath = Path.GetTempPath();
            }

            //workaround macOS symlinking its temp folder
            if (tempPath.StartsWith("/var"))
            {
                tempPath = "/private" + tempPath;
            }

            string testWorkingDirectory = Path.Combine(tempPath, "LibGit2Sharp-TestRepos");
            Trace.TraceInformation("Test working directory set to '{0}'", testWorkingDirectory);
            return testWorkingDirectory;
        }

        // To help with creating secure strings to test with.
        internal static SecureString StringToSecureString(string str)
        {
            var chars = str.ToCharArray();

            var secure = new SecureString();
            for (var i = 0; i < chars.Length; i++)
            {
                secure.AppendChar(chars[i]);
            }

            secure.MakeReadOnly();

            return secure;
        }
    }
}
