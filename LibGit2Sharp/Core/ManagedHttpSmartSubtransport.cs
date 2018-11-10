using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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

            private MemoryStream postBuffer = new MemoryStream();
            private Stream responseStream;

            private HttpClientHandler httpClientHandler;
            private HttpClient httpClient;

            public ManagedHttpSmartSubtransportStream(ManagedHttpSmartSubtransport parent, string endpointUrl, bool isPost, string contentType)
                : base(parent)
            {
                EndpointUrl = new Uri(endpointUrl);
                IsPost = isPost;
                ContentType = contentType;

                httpClientHandler = CreateClientHandler();
                httpClient = new HttpClient(httpClientHandler);
            }

            private HttpClientHandler CreateClientHandler()
            {
#if !NETFRAMEWORK
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.SslProtocols |= SslProtocols.Tls12;
                httpClientHandler.ServerCertificateCustomValidationCallback = CertificateValidationProxy;
#else
                var httpClientHandler = new WebRequestHandler();
                httpClientHandler.ServerCertificateValidationCallback = CertificateValidationProxy;

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif

                httpClientHandler.AllowAutoRedirect = false;

                return httpClientHandler;
            }

            private Uri EndpointUrl
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

            private bool CertificateValidationProxy(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
            {
                try
                {
                    int ret = SmartTransport.CertificateCheck(new CertificateX509(cert), (errors == SslPolicyErrors.None), EndpointUrl.Host);
                    Ensure.ZeroResult(ret);

                    return true;
                }
                catch(Exception e)
                {
                    SetError(e);
                    return false;
                }
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

            private HttpRequestMessage CreateRequest(Uri endpointUrl, bool isPost, string contentType)
            {
                var verb = isPost ? new HttpMethod("POST") : new HttpMethod("GET");
                var request = new HttpRequestMessage(verb, endpointUrl);
                request.Headers.Add("User-Agent", String.Format("git/2.0 ({0})", GetUserAgent()));
                request.Headers.Remove("Expect");

                return request;
            }

            private HttpResponseMessage GetResponseWithRedirects()
            {
                ICredentials credentials = null;
                var url = EndpointUrl;
                int retries;

                for (retries = 0; ; retries++)
                {
                    var httpClientHandler = CreateClientHandler();
                    httpClientHandler.Credentials = credentials;

                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        var request = CreateRequest(url, IsPost, ContentType);

                        if (retries > MAX_REDIRECTS)
                        {
                            throw new Exception("too many redirects or authentication replays");
                        }

                        if (IsPost && postBuffer.Length > 0)
                        {
                            var bufferDup = new MemoryStream(postBuffer.GetBuffer());
                            bufferDup.Seek(0, SeekOrigin.Begin);

                            request.Content = new StreamContent(bufferDup);
                            request.Content.Headers.Add("Content-Type", ContentType);
                        }

                        var response = httpClient.SendAsync(request).Result;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return response;
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            Credentials cred;
                            int ret = SmartTransport.AcquireCredentials(out cred, null, typeof(UsernamePasswordCredentials));

                            if (ret != 0)
                            {
                                throw new InvalidOperationException("authentication cancelled");
                            }

                            UsernamePasswordCredentials userpass = (UsernamePasswordCredentials)cred;
                            credentials = new NetworkCredential(userpass.Username, userpass.Password);
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
                    HttpResponseMessage response = GetResponseWithRedirects();
                    responseStream = response.Content.ReadAsStreamAsync().Result;
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

                if (httpClient != null)
                {
                    httpClient.Dispose();
                    httpClient = null;
                }

                base.Free();
            }
        }
    }
}
