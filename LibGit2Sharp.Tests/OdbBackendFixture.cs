using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class OdbBackendFixture : BaseFixture
    {
        private const string content = "test\n";

        private static void AddCommitToRepo(Repository repo)
        {
            string relativeFilepath = "test.txt";
            Touch(repo.Info.WorkingDirectory, relativeFilepath, content);
            repo.Index.Stage(relativeFilepath);

            var ie = repo.Index[relativeFilepath];
            Assert.NotNull(ie);
            Assert.Equal("9daeafb9864cf43055ae93beb0afd6c7d144bfa4", ie.Id.Sha);

            var author = new Signature("nulltoken", "emeric.fermas@gmail.com", DateTimeOffset.Parse("Wed, Dec 14 2011 08:29:03 +0100"));
            repo.Commit("Initial commit", author, author);

            relativeFilepath = "big.txt";
            var zeros = new string('0', 32*1024 + 3);
            Touch(repo.Info.WorkingDirectory, relativeFilepath, zeros);
            repo.Index.Stage(relativeFilepath);

            ie = repo.Index[relativeFilepath];
            Assert.NotNull(ie);
            Assert.Equal("6518215c4274845a759cb498998fe696c42e3e0f", ie.Id.Sha);
        }

        private static void AssertGeneratedShas(Repository repo)
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
                    Assert.Equal(0, backend.Write(fakeId.RawId, ms, dummy.Length, ObjectType.Blob));
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
                        OdbBackendOperations.Exists;
                }
            }

            public override int Read(byte[] oid, out Stream data, out ObjectType objectType)
            {
                data = null;
                objectType = default(ObjectType);

                MockGitObject gitObject;

                if (m_objectIdToContent.TryGetValue(oid, out gitObject))
                {
                    data = Allocate(gitObject.Length);

                    foreach (var chunk in gitObject.Data)
                    {
                        data.Write(chunk, 0, chunk.Length);
                    }

                    objectType = gitObject.ObjectType;

                    return GIT_OK;
                }

                return GIT_ENOTFOUND;
            }

            public override int ReadPrefix(byte[] shortOid, int len, out byte[] oid, out Stream data, out ObjectType objectType)
            {
                oid = null;
                data = null;
                objectType = default(ObjectType);

                MockGitObject gitObjectAlreadyFound = null;

                foreach (MockGitObject gitObject in m_objectIdToContent.Values)
                {
                    bool match = true;

                    int length = len >> 1;
                    for (int i = 0; i < length; i++)
                    {
                        if (gitObject.ObjectId[i] != shortOid[i])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match && ((len & 1) == 1))
                    {
                        var a = gitObject.ObjectId[length] >> 4;
                        var b = shortOid[length] >> 4;

                        if (a != b)
                        {
                            match = false;
                        }
                    }

                    if (!match)
                    {
                        continue;
                    }

                    if (null != gitObjectAlreadyFound)
                    {
                        return GIT_EAMBIGUOUS;
                    }

                    gitObjectAlreadyFound = gitObject;
                }

                if (null != gitObjectAlreadyFound)
                {
                    oid = gitObjectAlreadyFound.ObjectId;
                    objectType = gitObjectAlreadyFound.ObjectType;

                    data = Allocate(gitObjectAlreadyFound.Length);

                    foreach (var chunk in gitObjectAlreadyFound.Data)
                    {
                        data.Write(chunk, 0, chunk.Length);
                    }

                    return GIT_OK;
                }

                return GIT_ENOTFOUND;
            }

            public override int Write(byte[] oid, Stream dataStream, long length, ObjectType objectType)
            {
                if (m_objectIdToContent.ContainsKey(oid))
                {
                    return GIT_EEXISTS;
                }

                if (length > int.MaxValue)
                {
                    return GIT_ERROR;
                }

                var buffer = new byte[length];
                int bytesRead = dataStream.Read(buffer, 0, (int)length);

                if (bytesRead != (int)length)
                {
                    return GIT_ERROR;
                }

                m_objectIdToContent.Add(oid, new MockGitObject(oid, objectType, length, new List<byte[]> { buffer }));

                return GIT_OK;
            }

            public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
            {
                stream = new MockOdbBackendStream(this, objectType, length);

                return GIT_OK;
            }

            public override bool Exists(byte[] oid)
            {
                return m_objectIdToContent.ContainsKey(oid);
            }

            private readonly Dictionary<byte[], MockGitObject> m_objectIdToContent =
                new Dictionary<byte[], MockGitObject>(MockGitObjectComparer.Instance);

            private const int GIT_OK = 0;
            private const int GIT_ERROR = -1;
            private const int GIT_ENOTFOUND = -3;
            private const int GIT_EEXISTS = -4;
            private const int GIT_EAMBIGUOUS = -5;

            #region Unimplemented

            public override int ReadHeader(byte[] oid, out int length, out ObjectType objectType)
            {
                throw new NotImplementedException();
            }

            public override int ReadStream(byte[] oid, out OdbBackendStream stream)
            {
                throw new NotImplementedException();
            }

            public override int ForEach(ForEachCallback callback)
            {
                throw new NotImplementedException();
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
                    m_hash = new OdbHasher(objectType, length);
                }

                protected override void Dispose()
                {
                    m_hash.Dispose();

                    base.Dispose();
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
                    if (length > Int32.MaxValue)
                        return GIT_ERROR;

                    var buffer = new byte[length];

                    int bytesRead = dataStream.Read(buffer, 0, (int)length);

                    if (bytesRead != (int)length)
                        return GIT_ERROR;

                    m_hash.Update(buffer, (int)length);
                    m_chunks.Add(buffer);

                    return GIT_OK;
                }

                public override int FinalizeWrite(out byte[] oid)
                {
                    oid = null;

                    long totalLength = m_chunks.Sum(chunk => chunk.Length);

                    if (totalLength != m_length)
                    {
                        return GIT_ERROR;
                    }

                    oid = m_hash.RetrieveHash();

                    var backend = (MockOdbBackend)Backend;

                    if (!backend.m_objectIdToContent.ContainsKey(oid))
                    {
                        backend.m_objectIdToContent.Add(oid, new MockGitObject(oid, m_type, m_length, m_chunks));
                    }

                    return GIT_OK;
                }

                private readonly List<byte[]> m_chunks = new List<byte[]>();

                private readonly ObjectType m_type;
                private readonly long m_length;
                private readonly OdbHasher m_hash;

                #region Unimplemented

                public override int Read(Stream dataStream, long length)
                {
                    throw new NotImplementedException();
                }

                private class OdbHasher : IDisposable
                {
                    private readonly HashAlgorithm hasher;
                    private bool hashing = true;

                    public OdbHasher(ObjectType objectType, long length)
                    {
                        hasher = new SHA1CryptoServiceProvider();

                        string header = String.Format("{0} {1} ", ToHeaderFormat(objectType), length);

                        byte[] buffer = Encoding.ASCII.GetBytes(header);

                        buffer[buffer.Length - 1] = 0;
                        hasher.TransformBlock(buffer, 0, buffer.Length, null, 0);
                    }

                    public void Update(byte[] buffer, int length)
                    {
                        if (!hashing)
                        {
                            throw new InvalidOperationException();
                        }

                        hasher.TransformBlock(buffer, 0, length, null, 0);
                    }

                    public byte[] RetrieveHash()
                    {
                        hashing = false;

                        hasher.TransformFinalBlock(new byte[]{}, 0, 0);
                        return hasher.Hash;
                    }

                    private static string ToHeaderFormat(ObjectType type)
                    {
                        switch (type)
                        {
                            case ObjectType.Commit:
                                return "commit";

                            case ObjectType.Tree:
                                return "tree";

                            case ObjectType.Blob:
                                return "blob";

                            case ObjectType.Tag:
                                return "tag";

                            default:
                                throw new InvalidOperationException(String.Format("Cannot map {0} to a header format entry.", type));
                        }
                    }

                    public void Dispose()
                    {
                        ((IDisposable)hasher).Dispose();
                    }
                }

                #endregion
            }

            #endregion

            #region MockGitObject

            private class MockGitObject
            {
                public MockGitObject(byte[] objectId, ObjectType objectType, long length, List<byte[]> data)
                {
                    if (objectId.Length != 20)
                    {
                        throw new InvalidOperationException();
                    }

                    ObjectId = objectId;
                    ObjectType = objectType;
                    Data = data;
                    Length = length;
                }

                public readonly byte[] ObjectId;
                public readonly ObjectType ObjectType;
                public readonly List<byte[]> Data;
                public readonly long Length;
            }

            #endregion

            #region MockGitObjectComparer

            private class MockGitObjectComparer : IEqualityComparer<byte[]>
            {
                public bool Equals(byte[] x, byte[] y)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        if (x[i] != y[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }

                public int GetHashCode(byte[] obj)
                {
                    int toReturn = 0;

                    for (int i = 0; i < obj.Length / 4; i++)
                    {
                        toReturn ^= (int)obj[4 * i] << 24 +
                                    (int)obj[4 * i + 1] << 16 +
                                    (int)obj[4 * i + 2] << 8 +
                                    (int)obj[4 * i + 3];
                    }

                    return toReturn;
                }

                public static MockGitObjectComparer Instance
                {
                    get
                    {
                        if (null == s_instance)
                        {
                            s_instance = new MockGitObjectComparer();
                        }

                        return s_instance;
                    }
                }

                private static MockGitObjectComparer s_instance;
            }

            #endregion
        }

        #endregion
    }
}
