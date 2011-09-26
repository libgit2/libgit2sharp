﻿using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class Constants
    {
        public const string BareTestRepoPath = "./Resources/testrepo.git";
        public const string StandardTestRepoWorkingDirPath = "./Resources/testrepo_wd";
        public static string StandardTestRepoPath = Path.Combine(StandardTestRepoWorkingDirPath, ".git");
        public const string TemporaryReposPath = "TestRepos";
        public const string UnknownSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        public static readonly Signature Signature = new Signature("A. U. Thor", "thor@valhalla.asgard.com", new DateTimeOffset(2011, 06, 16, 10, 58, 27, TimeSpan.FromHours(2)));
    }
}
