using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Moq;
using Xunit;

namespace LibGit2Sharp.Tests
{
    // This fixture shows how one can mock the IRepository when writing an application against LibGit2Sharp.
    // The application we want to test is simulated by the CommitCounter class (see below), which takes an IRepository,
    // and whose role is to compute and return the number of commits in the given repository.
    public class MockedRepositoryFixture : BaseFixture
    {
        // In this test, we pass to CommitCounter a concrete instance of the Repository. It means we will end up calling the concrete Repository
        // during the test run.
        [Fact]
        public void CanCountCommitsWithConcreteRepository()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var commitCounter = new CommitCounter(repo);
                Assert.Equal(7, commitCounter.NumberOfCommits);
            }
        }

        // This test shows that CommitCounter can take a mocked instance of IRepository. It means we can test CommitCounter without
        // relying on the concrete repository. We are testing CommitCounter in isolation.
        [Fact]
        public void CanCountCommitsWithMockedRepository()
        {
            var commitLog = Mock.Of<CommitLog>(cl => cl.GetEnumerator() == FakeCommitLog(17));
            var repo = Mock.Of<IRepository>(r => r.Commits == commitLog);

            var commitCounter = new CommitCounter(repo);
            Assert.Equal(17, commitCounter.NumberOfCommits);
        }

        private static IEnumerator<Commit> FakeCommitLog(int size)
        {
            for (int i = 0; i < size; i++)
            {
                yield return FakeCommit(Guid.NewGuid().ToString());
            }
        }

        private static Commit FakeCommit(string sha)
        {
            var commitMock = new Mock<Commit>();
            commitMock.SetupGet(x => x.Sha).Returns(sha);

            return commitMock.Object;
        }

        // Simulated external application ;)
        private class CommitCounter
        {
            private readonly IRepository repo;

            public CommitCounter(IRepository repo)
            {
                this.repo = repo;
            }

            public int NumberOfCommits
            {
                get { return repo.Commits.Count(); }
            }
        }
    }
}
