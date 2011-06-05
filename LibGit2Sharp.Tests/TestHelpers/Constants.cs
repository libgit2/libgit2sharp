namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class Constants
    {
        public const string TestRepoPath = "./Resources/testrepo.git";
        public const string TestRepoWithWorkingDirRootPath = "./Resources/testrepo_wd";
        public const string TestRepoWithWorkingDirPath = TestRepoWithWorkingDirRootPath + "/.git";
        public const string TemporaryReposPath = "TestRepos";
        public const string UnknownSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
    }
}