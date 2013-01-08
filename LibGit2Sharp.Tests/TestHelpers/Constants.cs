using System;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class Constants
    {
        public const string TemporaryReposPath = "TestRepos";
        public const string UnknownSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        public static readonly Signature Signature = new Signature("A. U. Thor", "thor@valhalla.asgard.com", new DateTimeOffset(2011, 06, 16, 10, 58, 27, TimeSpan.FromHours(2)));

        // Populate these to turn on live credential tests
        public const string PrivateRepoUrl = "";
        public const string PrivateRepoUsername = "";
        public const string PrivateRepoPassword = "";
    }
}
