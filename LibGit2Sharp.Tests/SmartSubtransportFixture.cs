using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class SmartSubtransportFixture : BaseFixture
    {
        // Warning: this certification validation callback will accept *all*
        // SSL certificates without validation.  This is *only* for testing.
        // Do *NOT* enable this in production.
        private readonly RemoteCertificateValidationCallback certificateValidationCallback =
            (sender, certificate, chain, errors) => { return true; };

        [Theory]
        [InlineData("http", "http://github.com/libgit2/TestGitRepository")]
        [InlineData("https", "https://github.com/libgit2/TestGitRepository")]
        public void CustomSmartSubtransportTest(string scheme, string url)
        {
            string remoteName = "testRemote";

            var scd = BuildSelfCleaningDirectory();
            var repoPath = Repository.Init(scd.RootedDirectoryPath);

            SmartSubtransportRegistration<MockSmartSubtransport> registration = null;

            try
            {
                // Disable server certificate validation for testing.
                // Do *NOT* enable this in production.
                ServicePointManager.ServerCertificateValidationCallback = certificateValidationCallback;

                registration = GlobalSettings.RegisterSmartSubtransport<MockSmartSubtransport>(scheme);
                Assert.NotNull(registration);

                using (var repo = new Repository(scd.DirectoryPath))
                {
                    Remote remote = repo.Network.Remotes.Add(remoteName, url);

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
                    string[] expectedTagNames = { "blob", "commit_tree", "annotated_tag" };
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
                GlobalSettings.UnregisterSmartSubtransport(registration);

                ServicePointManager.ServerCertificateValidationCallback -= certificateValidationCallback;
            }
        }

        [Fact]
        public void CannotReregisterScheme()
        {
            SmartSubtransportRegistration<MockSmartSubtransport> httpRegistration =
                GlobalSettings.RegisterSmartSubtransport<MockSmartSubtransport>("http");

            try
            {
                Assert.Throws<EntryExistsException>(() =>
                    GlobalSettings.RegisterSmartSubtransport<MockSmartSubtransport>("http"));
            }
            finally
            {
                GlobalSettings.UnregisterSmartSubtransport(httpRegistration);
            }
        }

        [Fact]
        public void CannotUnregisterTwice()
        {
            SmartSubtransportRegistration<MockSmartSubtransport> httpRegistration =
                GlobalSettings.RegisterSmartSubtransport<MockSmartSubtransport>("http");

            GlobalSettings.UnregisterSmartSubtransport(httpRegistration);

            Assert.Throws<NotFoundException>(() =>
                GlobalSettings.UnregisterSmartSubtransport(httpRegistration));
        }

        private class MockSmartSubtransport : RpcSmartSubtransport
        {
            protected override SmartSubtransportStream Action(String url, GitSmartSubtransportAction action)
            {
                String endpointUrl, contentType = null;
                bool isPost = false;

                switch (action)
                {
                    case GitSmartSubtransportAction.UploadPackList:
                        endpointUrl = String.Concat(url, "/info/refs?service=git-upload-pack");
                        break;

                    case GitSmartSubtransportAction.UploadPack:
                        endpointUrl = String.Concat(url, "/git-upload-pack");
                        contentType = "application/x-git-upload-pack-request";
                        isPost = true;
                        break;

                    case GitSmartSubtransportAction.ReceivePackList:
                        endpointUrl = String.Concat(url, "/info/refs?service=git-receive-pack");
                        break;

                    case GitSmartSubtransportAction.ReceivePack:
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

                protected override void Free()
                {
                    if (responseStream != null)
                    {
                        responseStream.Dispose();
                        responseStream = null;
                    }

                    base.Free();
                }
            }
        }
    }
}
