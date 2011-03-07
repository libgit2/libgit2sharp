namespace LibGit2Sharp.Tests
{
    public class ReadOnlyRepositoryFixtureBase
    {
        private const string readOnlyGitRepository = "./Resources/testrepo.git";

        protected virtual string PathToRepository { get { return readOnlyGitRepository; } }
    }
}