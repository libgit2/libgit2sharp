using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

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

                using (var repo = new Repository(repoPath))
                {
                    repo.Network.Remotes.Add(remoteName, url);

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
                    Commands.Fetch(repo, remoteName, Array.Empty<string>(),
                        new FetchOptions { OnUpdateTips = expectedFetchState.RemoteUpdateTipsHandler, TagFetchMode = TagFetchMode.Auto },
                    null);

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

        //[Theory]
        //[InlineData("https", "https://bitbucket.org/libgit2/testgitrepository.git", "libgit3", "libgit3")]
        //public void CanUseCredentials(string scheme, string url, string user, string pass)
        //{
        //    string remoteName = "testRemote";

        //    var scd = BuildSelfCleaningDirectory();
        //    Repository.Init(scd.RootedDirectoryPath);

        //    SmartSubtransportRegistration<MockSmartSubtransport> registration = null;

        //    try
        //    {
        //        // Disable server certificate validation for testing.
        //        // Do *NOT* enable this in production.
        //        ServicePointManager.ServerCertificateValidationCallback = certificateValidationCallback;

        //        registration = GlobalSettings.RegisterSmartSubtransport<MockSmartSubtransport>(scheme);
        //        Assert.NotNull(registration);

        //        using (var repo = new Repository(scd.DirectoryPath))
        //        {
        //            repo.Network.Remotes.Add(remoteName, url);

        //            // Set up structures for the expected results
        //            // and verifying the RemoteUpdateTips callback.
        //            TestRemoteInfo expectedResults = TestRemoteInfo.TestRemoteInstance;
        //            ExpectedFetchState expectedFetchState = new ExpectedFetchState(remoteName);

        //            // Add expected branch objects
        //            foreach (KeyValuePair<string, ObjectId> kvp in expectedResults.BranchTips)
        //            {
        //                expectedFetchState.AddExpectedBranch(kvp.Key, ObjectId.Zero, kvp.Value);
        //            }

        //            // Perform the actual fetch
        //            Commands.Fetch(repo, remoteName, new string[0], new FetchOptions {
        //                OnUpdateTips = expectedFetchState.RemoteUpdateTipsHandler, TagFetchMode = TagFetchMode.Auto,
        //                CredentialsProvider = (_user, _valid, _hostname) => new UsernamePasswordCredentials() { Username = user, Password = pass },
        //            }, null);

        //            // Verify the expected
        //            expectedFetchState.CheckUpdatedReferences(repo);
        //        }
        //    }
        //    finally
        //    {
        //        GlobalSettings.UnregisterSmartSubtransport(registration);

        //        ServicePointManager.ServerCertificateValidationCallback -= certificateValidationCallback;
        //    }
        //}

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
            protected override SmartSubtransportStream Action(string url, GitSmartSubtransportAction action)
            {
                string endpointUrl, contentType = null;
                bool isPost = false;

                switch (action)
                {
                    case GitSmartSubtransportAction.UploadPackList:
                        endpointUrl = string.Concat(url, "/info/refs?service=git-upload-pack");
                        break;

                    case GitSmartSubtransportAction.UploadPack:
                        endpointUrl = string.Concat(url, "/git-upload-pack");
                        contentType = "application/x-git-upload-pack-request";
                        isPost = true;
                        break;

                    case GitSmartSubtransportAction.ReceivePackList:
                        endpointUrl = string.Concat(url, "/info/refs?service=git-receive-pack");
                        break;

                    case GitSmartSubtransportAction.ReceivePack:
                        endpointUrl = string.Concat(url, "/git-receive-pack");
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
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

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

                        try
                        {
                            response = (HttpWebResponse)request.GetResponse();
                        }
                        catch (WebException ex)
                        {
                            response = ex.Response as HttpWebResponse;
                            if (response.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Credentials cred;
                                int ret = SmartTransport.AcquireCredentials(out cred, null, typeof(UsernamePasswordCredentials));
                                if (ret != 0)
                                {
                                    throw new InvalidOperationException("dunno");
                                }

                                request = CreateWebRequest(EndpointUrl, IsPost, ContentType);
                                UsernamePasswordCredentials userpass = (UsernamePasswordCredentials)cred;
                                request.Credentials = new NetworkCredential(userpass.Username, userpass.Password);
                                continue;
                            }

                            // rethrow if it's not 401
                            throw;
                        }

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
