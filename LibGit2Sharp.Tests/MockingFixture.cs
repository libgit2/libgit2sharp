using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Moq;
using Xunit;

namespace LibGit2Sharp.Tests
{
    // This fixture shows how one can mock various LibGit2Sharp APIs.
    public class MockingFixture : BaseFixture
    {
        // The application we want to test is simulated by the CommitCounter class (see below), which takes an IRepository,
        // and whose role is to compute and return the number of commits in the given repository.

        // In this test, we pass to CommitCounter a concrete instance of the Repository. It means we will end up calling the concrete Repository
        // during the test run.
        [Fact]
        public void CanCountCommitsWithConcreteRepository()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            var commitLog = Mock.Of<IQueryableCommitLog>(cl => cl.GetEnumerator() == FakeCommitLog(17));
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

        [Fact]
        public void CanFakeConfigurationBuildSignature()
        {
            const string name = "name";
            const string email = "email";
            var now = DateTimeOffset.UtcNow;

            var fakeConfig = new Mock<Configuration>();
            fakeConfig.Setup(c => c.BuildSignature(now))
                      .Returns<DateTimeOffset>(t => new Signature(name, email, t));

            var sig = fakeConfig.Object.BuildSignature(now);
            Assert.Equal(name, sig.Name);
            Assert.Equal(email, sig.Email);
            Assert.Equal(now, sig.When);
        }

        [Fact]
        public void CanFakeEnumerationOfConfiguration()
        {
            var fakeConfig = new Mock<Configuration>();
            fakeConfig.Setup(c => c.GetEnumerator()).Returns(FakeEntries);

            Assert.Equal(2, fakeConfig.Object.Count());
        }

        private static IEnumerator<ConfigurationEntry<string>> FakeEntries()
        {
            yield return FakeConfigurationEntry("foo", "bar", ConfigurationLevel.Local);
            yield return FakeConfigurationEntry("baz", "quux", ConfigurationLevel.Global);
        }

        private static ConfigurationEntry<string> FakeConfigurationEntry(string key, string value, ConfigurationLevel level)
        {
            return new Mock<ConfigurationEntry<string>>(key, value, level).Object;
        }
    }
}
