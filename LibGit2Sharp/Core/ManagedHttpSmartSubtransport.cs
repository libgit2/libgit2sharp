using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace LibGit2Sharp.Core
{
    internal class ManagedHttpSmartSubtransport : RpcSmartSubtransport
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

            return new ManagedHttpSmartSubtransportStream(this, endpointUrl, isPost, contentType);
        }

        private class ManagedHttpSmartSubtransportStream : SmartSubtransportStream
        {
            private static int MAX_REDIRECTS = 7;

#if NETCOREAPP
            private static readonly SocketsHttpHandler httpHandler;
#else
            private static readonly HttpClientHandler httpHandler;
#endif

            private static readonly CredentialCache credentialCache;

            private MemoryStream postBuffer = new MemoryStream();
            private HttpResponseMessage response;
            private Stream responseStream;

            static ManagedHttpSmartSubtransportStream()
            {
#if NETCOREAPP
                httpHandler = new SocketsHttpHandler();
                httpHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(5);
#else
                httpHandler = new HttpClientHandler();
                httpHandler.SslProtocols |= SslProtocols.Tls12;
#endif

                httpHandler.AllowAutoRedirect = false;

                credentialCache = new CredentialCache();
                httpHandler.Credentials = credentialCache;
            }

            public ManagedHttpSmartSubtransportStream(ManagedHttpSmartSubtransport parent, string endpointUrl, bool isPost, string contentType)
                : base(parent)
            {
                EndpointUrl = new Uri(endpointUrl);
                IsPost = isPost;
                ContentType = contentType;
            }

            private HttpClient CreateHttpClient(HttpMessageHandler handler)
            {
                return new HttpClient(handler, false)
                {
                    DefaultRequestHeaders =
                    {
                        // This worked fine when it was on, but git.exe doesn't specify this header, so we don't either.
                        ExpectContinue = false,
                    },
                };
            }

            private Uri EndpointUrl { get; set; }

            private bool IsPost { get; set; }

            private string ContentType { get; set; }

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

            private string GetUserAgent()
            {
                string userAgent = GlobalSettings.GetUserAgent();

                if (string.IsNullOrEmpty(userAgent))
                {
                    userAgent = "LibGit2Sharp " + GlobalSettings.Version.InformationalVersion;
                }

                return userAgent;
            }

            private HttpRequestMessage CreateRequest(Uri endpointUrl, bool isPost)
            {
                var verb = isPost ? new HttpMethod("POST") : new HttpMethod("GET");
                var request = new HttpRequestMessage(verb, endpointUrl);
                request.Headers.Add("User-Agent", $"git/2.0 ({GetUserAgent()})");
                request.Headers.Remove("Expect");

                return request;
            }

            private HttpResponseMessage GetResponseWithRedirects()
            {
                var url = EndpointUrl;
                int retries;

                for (retries = 0; ; retries++)
                {
                    using (var httpClient = CreateHttpClient(httpHandler))
                    {
                        var request = CreateRequest(url, IsPost);

                        if (retries > MAX_REDIRECTS)
                        {
                            throw new Exception("too many redirects or authentication replays");
                        }

                        if (IsPost && postBuffer.Length > 0)
                        {
                            var bufferDup = new MemoryStream(postBuffer.GetBuffer(), 0, (int)postBuffer.Length);

                            request.Content = new StreamContent(bufferDup);
                            request.Content.Headers.Add("Content-Type", ContentType);
                        }

                        var response = httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).GetAwaiter().GetResult();

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return response;
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            int ret = SmartTransport.AcquireCredentials(out Credentials cred, null, typeof(UsernamePasswordCredentials));

                            if (ret != 0)
                            {
                                throw new InvalidOperationException("authentication cancelled");
                            }

                            var scheme = response.Headers.WwwAuthenticate.First().Scheme;

                            if (cred is DefaultCredentials)
                            {
                                lock (credentialCache)
                                {
                                    credentialCache.Add(url, scheme, CredentialCache.DefaultNetworkCredentials);
                                }
                            }
                            else if (cred is UsernamePasswordCredentials userpass)
                            {
                                lock (credentialCache)
                                {
                                    credentialCache.Add(url, scheme, new NetworkCredential(userpass.Username, userpass.Password));
                                }
                            }

                            continue;
                        }
                        else if (response.StatusCode == HttpStatusCode.Moved || response.StatusCode == HttpStatusCode.Redirect)
                        {
                            url = new Uri(response.Headers.GetValues("Location").First());
                            continue;
                        }

                        throw new Exception(string.Format("unexpected HTTP response: {0}", response.StatusCode));
                    }
                }

                throw new Exception("too many redirects or authentication replays");
            }

            public override int Read(Stream dataStream, long length, out long readTotal)
            {
                byte[] buffer = new byte[4096];
                readTotal = 0;

                if (responseStream == null)
                {
                    response = GetResponseWithRedirects();
                    responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                }

                while (length > 0)
                {
                    int readLen = responseStream.Read(buffer, 0, (int)Math.Min(buffer.Length, length));

                    if (readLen == 0)
                    {
                        break;
                    }

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

                if (response != null)
                {
                    response.Dispose();
                    response = null;
                }

                base.Free();
            }
        }
    }
}
