using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class OdbBackendFixture : BaseFixture
    {
        [Fact]
        public void SimpleOdbBackendFixtureTest()
        {
            SelfCleaningDirectory scd = new SelfCleaningDirectory(this);

            Repository.Init(scd.RootedDirectoryPath);

            using (Repository repository = new Repository(scd.RootedDirectoryPath))
            {
                repository.ObjectDatabase.AddBackend(new MockOdbBackend(), priority: 5);

                String filePath = Path.Combine(scd.RootedDirectoryPath, "file.txt");
                String fileContents = "Hello!";

                // Exercises read, write, writestream, exists
                File.WriteAllText(filePath, fileContents);
                repository.Index.Stage(filePath);

                Signature signature = new Signature("SimpleOdbBackendFixtureTest", "user@example.com", DateTimeOffset.Now);
                repository.Commit(String.Empty, signature, signature);

                // Exercises read
                Blob blob = repository.Lookup(new ObjectId("69342c5c39e5ae5f0077aecc32c0f81811fb8193")) as Blob;

                Assert.NotNull(blob);
                Assert.True(fileContents.Length == blob.Size);
            }
        }

        #region MockOdbBackend

        private class MockOdbBackend : OdbBackend
        {
            public MockOdbBackend()
            {
            }

            protected override OdbBackend.OdbBackendOperations SupportedOperations
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

            public override int Read(byte[] oid, out Stream data, out GitObjectType objectType)
            {
                data = null;
                objectType = GitObjectType.Bad;

                MockGitObject gitObject;

                if (m_objectIdToContent.TryGetValue(oid, out gitObject))
                {
                    data = Allocate(gitObject.Data.LongLength);
                    data.Write(gitObject.Data, 0, gitObject.Data.Length);

                    objectType = gitObject.ObjectType;

                    return GIT_OK;
                }

                return GIT_ENOTFOUND;
            }

            public override int ReadPrefix(byte[] shortOid, out byte[] oid, out Stream data, out GitObjectType objectType)
            {
                oid = null;
                data = null;
                objectType = GitObjectType.Bad;

                MockGitObject gitObjectAlreadyFound = null;

                foreach (MockGitObject gitObject in m_objectIdToContent.Values)
                {
                    bool match = true;

                    for (int i = 0; i < shortOid.Length; i++)
                    {
                        if (gitObject.ObjectId[i] != shortOid[i])
                        {
                            match = false;
                            break;
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

                    data = Allocate(gitObjectAlreadyFound.Data.LongLength);
                    data.Write(gitObjectAlreadyFound.Data, 0, gitObjectAlreadyFound.Data.Length);

                    return GIT_OK;
                }

                return GIT_ENOTFOUND;
            }

            public override int Write(byte[] oid, Stream dataStream, long length, GitObjectType objectType, out byte[] finalOid)
            {
                using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
                {
                    finalOid = sha1.ComputeHash(dataStream);

                    dataStream.Seek(0, SeekOrigin.Begin);
                }

                if (m_objectIdToContent.ContainsKey(finalOid))
                {
                    return GIT_EEXISTS;
                }

                if (length > (long)int.MaxValue)
                {
                    return GIT_ERROR;
                }

                byte[] buffer = new byte[length];
                int bytesRead = dataStream.Read(buffer, 0, (int)length);

                if (bytesRead != (int)length)
                {
                    return GIT_ERROR;
                }

                m_objectIdToContent.Add(finalOid, new MockGitObject(finalOid, objectType, buffer));

                return GIT_OK;
            }

            public override int WriteStream(long length, GitObjectType objectType, out OdbBackendStream stream)
            {
                stream = new MockOdbBackendStream(this, objectType, length);

                return GIT_OK;
            }

            public override bool Exists(byte[] oid)
            {
                return m_objectIdToContent.ContainsKey(oid);
            }

            private Dictionary<byte[], MockGitObject> m_objectIdToContent = new Dictionary<byte[], MockGitObject>(MockGitObjectComparer.Instance);

            private const int GIT_OK = 0;
            private const int GIT_ERROR = -1;
            private const int GIT_ENOTFOUND = -3;
            private const int GIT_EEXISTS = -4;
            private const int GIT_EAMBIGUOUS = -5;

            #region Unimplemented

            public override int ReadHeader(byte[] oid, out int length, out GitObjectType objectType)
            {
                throw new NotImplementedException();
            }

            public override int ReadStream(byte[] oid, out OdbBackendStream stream)
            {
                throw new NotImplementedException();
            }

            public override int ForEach(OdbBackend.ForEachCallback callback)
            {
                throw new NotImplementedException();
            }

            #endregion

            #region MockOdbBackendStream

            private class MockOdbBackendStream : OdbBackendStream
            {
                public MockOdbBackendStream(MockOdbBackend backend, GitObjectType objectType, long length)
                    : base(backend)
                {
                    m_type = objectType;
                    m_length = length;
                    m_hash = new SHA1CryptoServiceProvider();
                }

                protected override void Dispose()
                {
                    ((IDisposable)m_hash).Dispose();

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
                    if (null == m_buffer)
                    {
                        m_buffer = new byte[length];

                        if (length > (long)int.MaxValue)
                            return GIT_ERROR;

                        int bytesRead = dataStream.Read(m_buffer, 0, (int)length);

                        if (bytesRead != (int)length)
                            return GIT_ERROR;

                        m_hash.TransformBlock(m_buffer, 0, (int)length, null, 0);
                    }
                    else
                    {
                        long newLength = m_buffer.LongLength + length;

                        if (newLength > (long)int.MaxValue)
                            return GIT_ERROR;

                        byte[] newBuffer = new byte[newLength];
                        Array.Copy(m_buffer, newBuffer, m_buffer.Length);

                        int bytesRead = dataStream.Read(newBuffer, m_buffer.Length, (int)length);

                        if (bytesRead != (int)length)
                            return GIT_ERROR;

                        m_hash.TransformBlock(newBuffer, m_buffer.Length, (int)length, null, 0);

                        m_buffer = newBuffer;
                    }

                    return GIT_OK;
                }

                public override int FinalizeWrite(out byte[] oid)
                {
                    m_hash.TransformFinalBlock(m_buffer, 0, 0);
                    oid = m_hash.Hash;

                    if (m_buffer.Length != (int)m_length)
                    {
                        return GIT_ERROR;
                    }

                    MockOdbBackend backend = (MockOdbBackend)this.Backend;

                    if (!backend.m_objectIdToContent.ContainsKey(oid))
                    {
                        backend.m_objectIdToContent.Add(oid, new MockGitObject(oid, m_type, m_buffer));
                    }

                    return GIT_OK;
                }

                private byte[] m_buffer;

                private readonly GitObjectType m_type;
                private readonly long m_length;
                private readonly HashAlgorithm m_hash;

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
                public MockGitObject(byte[] objectId, GitObjectType objectType, byte[] data)
                {
                    if (objectId.Length != 20)
                    {
                        throw new InvalidOperationException();
                    }

                    this.ObjectId = objectId;
                    this.ObjectType = objectType;
                    this.Data = data;
                }

                public byte[] ObjectId;
                public GitObjectType ObjectType;
                public byte[] Data;

                public int Length
                {
                    get
                    {
                        return this.Data.Length;
                    }
                }
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
