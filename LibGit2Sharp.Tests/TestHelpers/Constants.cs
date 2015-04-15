using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class Constants
    {
        public static readonly string TemporaryReposPath = BuildPath();
        public const string UnknownSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        public static readonly Identity Identity = new Identity("A. U. Thor", "thor@valhalla.asgard.com");
        public static readonly Signature Signature = new Signature(Identity, new DateTimeOffset(2011, 06, 16, 10, 58, 27, TimeSpan.FromHours(2)));

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
        // public const string PrivateRepoUrl = "https://tfs.contoso.com/tfs/DefaultCollection/project/_git/project";
        // ... return new DefaultCredentials();

        public const string PrivateRepoUrl = "";

        public static Credentials PrivateRepoCredentials(string url, string usernameFromUrl,
                                                         SupportedCredentialTypes types)
        {
            return null;
        }

        public static string BuildPath()
        {
            string tempPath;

            var unixPath = Type.GetType("Mono.Unix.UnixPath, Mono.Posix, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");

            if (unixPath != null)
            {
                // We're running on Mono/*nix. Let's unwrap the path
                tempPath = (string)unixPath.InvokeMember("GetCompleteRealPath",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy |
                    System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public,
                    null, unixPath, new object[] { Path.GetTempPath() });
            }
            else
            {
                // We're running on .Net/Windows
                tempPath = Path.GetTempPath();
            }

            return Path.Combine(tempPath, "LibGit2Sharp-TestRepos");
        }
    }
}
