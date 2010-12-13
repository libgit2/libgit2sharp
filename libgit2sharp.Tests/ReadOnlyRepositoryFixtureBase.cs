namespace libgit2sharp.Tests
{
    public class ReadOnlyRepositoryFixtureBase
    {
        protected virtual string PathToRepository  { get { return "../../Resources/testrepo.git"; } }
    }
}