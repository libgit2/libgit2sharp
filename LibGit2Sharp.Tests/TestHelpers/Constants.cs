﻿using System;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class Constants
    {
        public const string TemporaryReposPath = "TestRepos";
        public const string UnknownSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        public static readonly Signature Signature = new Signature("A. U. Thor", "thor@valhalla.asgard.com", new DateTimeOffset(2011, 06, 16, 10, 58, 27, TimeSpan.FromHours(2)));

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
    }
}
