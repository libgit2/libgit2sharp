using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using LibGit2Sharp;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class TransportFixture : BaseFixture
    {
        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        public void RemoteSmartSubtransportTest(string url)
        {
            string remoteName = "testRemote";

            var scd = BuildSelfCleaningDirectory();
            var repoPath = Repository.Init(scd.RootedDirectoryPath);

            RemoteCertificateValidationCallback certValidationCallback = (o, certificate, chain, errors) => true;

            try
            {
                /* The version of Mono that Travis uses is a sooooo oooold
                 * that even things like SSL certificate validation are broken.
                 *
                 * Our goal here isn't to test certificate validation, it's to
                 * test that we can set this transport.  Just allow any old cert.
                 */
                ServicePointManager.ServerCertificateValidationCallback += certValidationCallback;

                using (var repo = new Repository(scd.DirectoryPath))
                {
                    Remote remote = repo.Network.Remotes.Add(remoteName, url);
                    remote.Transport = new MockSmartSubtransport();

                    // Set up structures for the expected results
                    // and verifying the RemoteUpdateTips callback.
                    TestRemoteInfo expectedResults = TestRemoteInfo.TestRemoteInstance;
                    ExpectedFetchState expectedFetchState = new ExpectedFetchState(remoteName);

                    // Add expected branch objects
                    foreach (KeyValuePair<string, ObjectId> kvp in expectedResults.BranchTips)
                    {
                        expectedFetchState.AddExpectedBranch(kvp.Key, ObjectId.Zero, kvp.Value);
                    }

                    // Add the expected tags
                    string[] expectedTagNames = { "blob", "commit_tree" };
                    foreach (string tagName in expectedTagNames)
                    {
                        TestRemoteInfo.ExpectedTagInfo expectedTagInfo = expectedResults.Tags[tagName];
                        expectedFetchState.AddExpectedTag(tagName, ObjectId.Zero, expectedTagInfo);
                    }

                    // Perform the actual fetch
                    repo.Network.Fetch(remote, new FetchOptions { OnUpdateTips = expectedFetchState.RemoteUpdateTipsHandler, TagFetchMode = TagFetchMode.Auto });

                    // Verify the expected
                    expectedFetchState.CheckUpdatedReferences(repo);
                }
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback -= certValidationCallback;
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        public void SmartSubtransportPropogatesErrors(string url)
        {
            string remoteName = "testRemote";

            var scd = BuildSelfCleaningDirectory();
            var repoPath = Repository.Init(scd.RootedDirectoryPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);
                remote.Transport = new FailSmartSubtransport();

                Assert.Throws<LibGit2SharpException>(() => repo.Network.Fetch(remote));
            }
        }

        [RpcSmartSubtransport]
        private class MockSmartSubtransport : SmartSubtransport
        {
            protected override SmartSubtransportStream Action(String url, SmartSubtransportAction action)
            {
                String endpointUrl, contentType = null;
                bool isPost = false;

                switch (action)
                {
                    case SmartSubtransportAction.UploadPackList:
                        endpointUrl = String.Concat(url, "/info/refs?service=git-upload-pack");
                        break;

                    case SmartSubtransportAction.UploadPack:
                        endpointUrl = String.Concat(url, "/git-upload-pack");
                        contentType = "application/x-git-upload-pack-request";
                        isPost = true;
                        break;

                    case SmartSubtransportAction.ReceivePackList:
                        endpointUrl = String.Concat(url, "/info/refs?service=git-receive-pack");
                        break;

                    case SmartSubtransportAction.ReceivePack:
                        endpointUrl = String.Concat(url, "/git-receive-pack");
                        contentType = "application/x-git-receive-pack-request";
                        isPost = true;
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                return new MockSmartSubtransportStream(this, endpointUrl, isPost, contentType);
            }

            private class MockSmartSubtransportStream : SmartSubtransportStream
            {
                private static int MAX_REDIRECTS = 5;

                private MemoryStream postBuffer = new MemoryStream();
                private Stream responseStream;

                public MockSmartSubtransportStream(MockSmartSubtransport parent, string endpointUrl, bool isPost, string contentType)
                    : base(parent)
                {
                    EndpointUrl = endpointUrl;
                    IsPost = isPost;
                    ContentType = contentType;
                }

                private string EndpointUrl
                {
                    get;
                    set;
                }

                private bool IsPost
                {
                    get;
                    set;
                }

                private string ContentType
                {
                    get;
                    set;
                }

                public override int Write(Stream dataStream, long length)
                {
                    byte[] buffer = new byte[4096];
                    long writeTotal = 0;

                    while (length > 0)
                    {
                        int readLen = dataStream.Read(buffer, 0, (int)Math.Min(buffer.Length, length));

                        if (readLen == 0)
                        {
                            break;
                        }

                        postBuffer.Write(buffer, 0, readLen);
                        length -= readLen;
                        writeTotal += readLen;
                    }

                    if (writeTotal < length)
                    {
                        throw new EndOfStreamException("Could not write buffer (short read)");
                    }

                    return 0;
                }

                private static HttpWebRequest CreateWebRequest(string endpointUrl, bool isPost, string contentType)
                {
                    HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(endpointUrl);
                    webRequest.UserAgent = "git/1.0 (libgit2 custom transport)";
                    webRequest.ServicePoint.Expect100Continue = false;
                    webRequest.AllowAutoRedirect = false;

                    if (isPost)
                    {
                        webRequest.Method = "POST";
                        webRequest.ContentType = contentType;
                    }

                    return webRequest;
                }

                private HttpWebResponse GetResponseWithRedirects()
                {
                    HttpWebRequest request = CreateWebRequest(EndpointUrl, IsPost, ContentType);
                    HttpWebResponse response = null;

                    for (int i = 0; i < MAX_REDIRECTS; i++)
                    {
                        if (IsPost && postBuffer.Length > 0)
                        {
                            postBuffer.Seek(0, SeekOrigin.Begin);

                            using (Stream requestStream = request.GetRequestStream())
                            {
                                postBuffer.WriteTo(requestStream);
                            }
                        }

                        response = (HttpWebResponse)request.GetResponse();

                        if (response.StatusCode == HttpStatusCode.Moved || response.StatusCode == HttpStatusCode.Redirect)
                        {
                            request = CreateWebRequest(response.Headers["Location"], IsPost, ContentType);
                            continue;
                        }

                        break;
                    }

                    if (response == null)
                    {
                        throw new Exception("Too many redirects");
                    }

                    return response;
                }

                public override int Read(Stream dataStream, long length, out long readTotal)
                {
                    byte[] buffer = new byte[4096];
                    readTotal = 0;

                    if (responseStream == null)
                    {
                        HttpWebResponse response = GetResponseWithRedirects();
                        responseStream = response.GetResponseStream();
                    }

                    while (length > 0)
                    {
                        int readLen = responseStream.Read(buffer, 0, (int)Math.Min(buffer.Length, length));

                        if (readLen == 0)
                            break;

                        dataStream.Write(buffer, 0, readLen);
                        readTotal += readLen;
                        length -= readLen;
                    }

                    return 0;
                }

                protected override void Dispose()
                {
                    if (responseStream != null)
                    {
                        responseStream.Dispose();
                        responseStream = null;
                    }

                    base.Dispose();
                }
            }
        }


        [RpcSmartSubtransport]
        private class FailSmartSubtransport : SmartSubtransport
        {
            protected override SmartSubtransportStream Action(String url, SmartSubtransportAction action)
            {
                throw new LibGit2SharpException("Failure!");
            }
        }
    }
}
