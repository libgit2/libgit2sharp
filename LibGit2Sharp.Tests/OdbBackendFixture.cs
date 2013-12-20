using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class OdbBackendFixture : BaseFixture
    {
        private const string content = "test\n";

        private static Commit AddCommitToRepo(IRepository repo)
        {
            string relativeFilepath = "test.txt";
            Touch(repo.Info.WorkingDirectory, relativeFilepath, content);
            repo.Index.Stage(relativeFilepath);

            var ie = repo.Index[relativeFilepath];
            Assert.NotNull(ie);
            Assert.Equal("9daeafb9864cf43055ae93beb0afd6c7d144bfa4", ie.Id.Sha);

            var author = new Signature("nulltoken", "emeric.fermas@gmail.com", DateTimeOffset.Parse("Wed, Dec 14 2011 08:29:03 +0100"));
            var commit = repo.Commit("Initial commit", author, author);

            relativeFilepath = "big.txt";
            var zeros = new string('0', 32*1024 + 3);
            Touch(repo.Info.WorkingDirectory, relativeFilepath, zeros);
            repo.Index.Stage(relativeFilepath);

            ie = repo.Index[relativeFilepath];
            Assert.NotNull(ie);
            Assert.Equal("6518215c4274845a759cb498998fe696c42e3e0f", ie.Id.Sha);

            return commit;
        }

        private static void AssertGeneratedShas(IRepository repo)
        {
            Commit commit = repo.Commits.Single();
            Assert.Equal("1fe3126578fc4eca68c193e4a3a0a14a0704624d", commit.Sha);
            Tree tree = commit.Tree;
            Assert.Equal("2b297e643c551e76cfa1f93810c50811382f9117", tree.Sha);

            GitObject blob = tree.Single().Target;
            Assert.IsAssignableFrom<Blob>(blob);
            Assert.Equal("9daeafb9864cf43055ae93beb0afd6c7d144bfa4", blob.Sha);
        }

        [Fact]
        public void CanGeneratePredictableObjectShasWithTheDefaultBackend()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                AddCommitToRepo(repo);

                AssertGeneratedShas(repo);
            }
        }

        [Fact]
        public void CanGeneratePredictableObjectShasWithAProvidedBackend()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                repo.ObjectDatabase.AddBackend(new MockOdbBackend(), priority: 5);

                AddCommitToRepo(repo);

                AssertGeneratedShas(repo);

                var objectId = new ObjectId("9daeafb9864cf43055ae93beb0afd6c7d144bfa4");

                Assert.True(repo.ObjectDatabase.Contains(objectId));

                var blob = repo.Lookup<Blob>(objectId);
                Assert.True(content.Length == blob.Size);

                var other = repo.Lookup<Blob>("9daeaf");
                Assert.Equal(blob, other);
            }
        }

        [Fact]
        public void CanRetrieveObjectsThroughOddSizedShortShas()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                var backend = new MockOdbBackend();
                repo.ObjectDatabase.AddBackend(backend, priority: 5);

                AddCommitToRepo(repo);

                var blob1 = repo.Lookup<Blob>("9daeaf");
                Assert.NotNull(blob1);

                const string dummy = "dummy\n";

                // Inserts a fake blob with a similarly prefixed sha
                var fakeId = new ObjectId("9daeaf0000000000000000000000000000000000");
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(dummy)))
                {
                    Assert.Equal(0, backend.Write(fakeId, ms, dummy.Length, ObjectType.Blob));
                }

                var blob2 = repo.Lookup<Blob>(fakeId);
                Assert.NotNull(blob2);

                Assert.Throws<AmbiguousSpecificationException>(() => repo.Lookup<Blob>("9daeaf"));

                var newBlob1 = repo.Lookup("9daeafb");
                var newBlob2 = repo.Lookup("9daeaf0");

                Assert.Equal(blob1, newBlob1);
                Assert.Equal(blob2, newBlob2);
            }
        }

        [Fact]
        public void CanEnumerateTheContentOfTheObjectDatabase()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                var backend = new MockOdbBackend();
                repo.ObjectDatabase.AddBackend(backend, priority: 5);

                AddCommitToRepo(repo);

                var expected = new[]{ "1fe3126", "2b297e6", "6518215", "9daeafb" };

                IEnumerable<GitObject> objs = repo.ObjectDatabase;

                IEnumerable<string> retrieved =
                    objs
                    .Select(o => o.Id.ToString(7))
                    .OrderBy(s => s, StringComparer.Ordinal);

                Assert.Equal(expected, retrieved);
            }
        }

        [Fact]
        public void CanPushWithACustomBackend()
        {
            string remoteRepoPath = InitNewRepository(true);
            string localRepoPath = InitNewRepository();
            Commit commit;

            using (var localRepo = new Repository(localRepoPath))
            {
                localRepo.ObjectDatabase.AddBackend(new MockOdbBackend(), 5);

                commit = AddCommitToRepo(localRepo);

                Remote remote = localRepo.Network.Remotes.Add("origin", remoteRepoPath);

                localRepo.Branches.Update(localRepo.Head,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = localRepo.Head.CanonicalName);

                localRepo.Network.Push(localRepo.Head);
            }

            using (var remoteRepo = new Repository(remoteRepoPath))
            {
                Assert.Equal(commit, remoteRepo.Head.Tip);
            }
        }

        #region MockOdbBackend

        private class MockOdbBackend : OdbBackend
        {
            protected override OdbBackendOperations SupportedOperations
            {
                get
                {
                    return OdbBackendOperations.Read |
                        OdbBackendOperations.ReadPrefix |
                        OdbBackendOperations.Write |
                        OdbBackendOperations.WriteStream |
                        OdbBackendOperations.Exists |
                        OdbBackendOperations.ForEach |
                        OdbBackendOperations.ReadHeader;
                }
            }

            public override int Read(ObjectId oid, out Stream data, out ObjectType objectType)
            {
                data = null;
                objectType = default(ObjectType);

                MockGitObject gitObject;

                if (!m_objectIdToContent.TryGetValue(oid, out gitObject))
                {
                    return (int) ReturnCode.GIT_ENOTFOUND;
                }

                data = Allocate(gitObject.Length);

                foreach (var chunk in gitObject.Data)
                {
                    data.Write(chunk, 0, chunk.Length);
                }

                objectType = gitObject.ObjectType;

                return (int)ReturnCode.GIT_OK;
            }

            public override int ReadPrefix(string shortSha, out ObjectId id, out Stream data, out ObjectType objectType)
            {
                id = null;
                data = null;
                objectType = default(ObjectType);

                ObjectId matchingKey = null;

                foreach (ObjectId objectId in m_objectIdToContent.Keys)
                {
                    if (!objectId.StartsWith(shortSha))
                    {
                        continue;
                    }

                    if (matchingKey != null)
                    {
                        return (int)ReturnCode.GIT_EAMBIGUOUS;
                    }

                    matchingKey = objectId;
                }

                if (matchingKey == null)
                {
                    return (int)ReturnCode.GIT_ENOTFOUND;
                }

                int ret = Read(matchingKey, out data, out objectType);

                if (ret != (int)ReturnCode.GIT_OK)
                {
                    return ret;
                }

                id = matchingKey;

                return (int)ReturnCode.GIT_OK;
            }

            public override int Write(ObjectId oid, Stream dataStream, long length, ObjectType objectType)
            {
                var buffer = ReadBuffer(dataStream, length);

                m_objectIdToContent.Add(oid,
                    new MockGitObject(oid, objectType, length, new List<byte[]> { buffer }));

                return (int)ReturnCode.GIT_OK;
            }

            private static byte[] ReadBuffer(Stream dataStream, long length)
            {
                if (length > int.MaxValue)
                {
                    throw new InvalidOperationException(
                        string.Format("Provided length ({0}) exceeds int.MaxValue ({1}).", length, int.MaxValue));
                }

                var buffer = new byte[length];
                int bytesRead = dataStream.Read(buffer, 0, (int)length);

                if (bytesRead != (int)length)
                {
                    throw new InvalidOperationException(
                        string.Format("Too short buffer. {0} bytes were expected. {1} have been successfully read.", length,
                            bytesRead));
                }
                return buffer;
            }

            public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
            {
                stream = new MockOdbBackendStream(this, objectType, length);

                return (int)ReturnCode.GIT_OK;
            }

            public override bool Exists(ObjectId oid)
            {
                return m_objectIdToContent.ContainsKey(oid);
            }

            public override int ReadHeader(ObjectId oid, out int length, out ObjectType objectType)
            {
                objectType = default(ObjectType);
                length = 0;

                MockGitObject gitObject;

                if (!m_objectIdToContent.TryGetValue(oid, out gitObject))
                {
                    return (int)ReturnCode.GIT_ENOTFOUND;
                }

                objectType = gitObject.ObjectType;
                length = (int)gitObject.Length;

                return (int)ReturnCode.GIT_OK;
            }

            private readonly Dictionary<ObjectId, MockGitObject> m_objectIdToContent =
                new Dictionary<ObjectId, MockGitObject>();

            #region Unimplemented

            public override int ReadStream(ObjectId oid, out OdbBackendStream stream)
            {
                throw new NotImplementedException();
            }

            public override int ForEach(ForEachCallback callback)
            {
                foreach (var objectId in m_objectIdToContent.Keys)
                {
                    int result = callback(objectId);

                    if (result != (int)ReturnCode.GIT_OK)
                    {
                        return result;
                    }
                }

                return (int)ReturnCode.GIT_OK;
            }

            #endregion

            #region MockOdbBackendStream

            private class MockOdbBackendStream : OdbBackendStream
            {
                public MockOdbBackendStream(MockOdbBackend backend, ObjectType objectType, long length)
                    : base(backend)
                {
                    m_type = objectType;
                    m_length = length;
                }

                public override bool CanRead
                {
                    get
                    {
                        return false;
                    }
                }

                public override bool CanWrite
                {
                    get
                    {
                        return true;
                    }
                }

                public override int Write(Stream dataStream, long length)
                {
                    var buffer = ReadBuffer(dataStream, length);

                    m_chunks.Add(buffer);

                    return (int)ReturnCode.GIT_OK;
                }

                public override int FinalizeWrite(ObjectId oid)
                {
                    long totalLength = m_chunks.Sum(chunk => chunk.Length);

                    if (totalLength != m_length)
                    {
                        throw new InvalidOperationException(
                            string.Format("Invalid final length. {0} was expected. The total size of the received chunks is {1}.", m_length, totalLength));
                    }

                    var backend = (MockOdbBackend)Backend;

                    backend.m_objectIdToContent.Add(oid,
                        new MockGitObject(oid, m_type, m_length, m_chunks));

                    return (int)ReturnCode.GIT_OK;
                }

                private readonly List<byte[]> m_chunks = new List<byte[]>();

                private readonly ObjectType m_type;
                private readonly long m_length;

                #region Unimplemented

                public override int Read(Stream dataStream, long length)
                {
                    throw new NotImplementedException();
                }

                #endregion
            }

            #endregion

            #region MockGitObject

            private class MockGitObject
            {
                public MockGitObject(ObjectId objectId, ObjectType objectType, long length, List<byte[]> data)
                {
                    ObjectId = objectId;
                    ObjectType = objectType;
                    Data = data;
                    Length = length;
                }

                public readonly ObjectId ObjectId;
                public readonly ObjectType ObjectType;
                public readonly List<byte[]> Data;
                public readonly long Length;
            }

            #endregion
        }

        #endregion
    }
}
