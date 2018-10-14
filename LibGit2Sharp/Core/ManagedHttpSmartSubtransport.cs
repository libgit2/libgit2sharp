using System;
using System.IO;
using System.Net;
using System.Net.Security;
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

            public ManagedHttpSmartSubtransportStream(ManagedHttpSmartSubtransport parent, string endpointUrl, bool isPost, string contentType)
                : base(parent)
            {
                EndpointUrl = new Uri(endpointUrl);
                IsPost = isPost;
                ContentType = contentType;
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
                int ret = SmartTransport.CertificateCheck(new CertificateX509(cert), (errors == SslPolicyErrors.None), EndpointUrl.Host);
                Ensure.ZeroResult(ret);

                return true;
            }

            private string getUserAgent()
            {
                string userAgent = GlobalSettings.GetUserAgent();

                if (string.IsNullOrEmpty(userAgent))
                {
                    userAgent = "LibGit2Sharp " + GlobalSettings.Version.InformationalVersion;
                }

                return userAgent;
            }

            private HttpWebRequest CreateWebRequest(Uri endpointUrl, bool isPost, string contentType)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(endpointUrl);
                webRequest.UserAgent = String.Format("git/2.0 ({0})", getUserAgent());
                webRequest.ServicePoint.Expect100Continue = false;
                webRequest.AllowAutoRedirect = false;
                webRequest.ServerCertificateValidationCallback += CertificateValidationProxy;

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
                int retries;

                for (retries = 0; ; retries++)
                {
                    if (retries > MAX_REDIRECTS)
                    {
                        throw new Exception("too many redirects or authentication replays");
                    }

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
                        if (ex.Response != null)
                        {
                            response = (HttpWebResponse)ex.Response;
                        }
                        else if (ex.InnerException != null)
                        {
                            throw ex.InnerException;
                        }
                        else
                        {
                            throw new Exception("unknown network failure");
                        }
                    }

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        break;
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Credentials cred;
                        int ret = SmartTransport.AcquireCredentials(out cred, null, typeof(UsernamePasswordCredentials));

                        if (ret != 0)
                        {
                            throw new InvalidOperationException("authentication cancelled");
                        }

                        request = CreateWebRequest(EndpointUrl, IsPost, ContentType);
                        UsernamePasswordCredentials userpass = (UsernamePasswordCredentials)cred;
                        request.Credentials = new NetworkCredential(userpass.Username, userpass.Password);
                        continue;
                    }
                    else if (response.StatusCode == HttpStatusCode.Moved || response.StatusCode == HttpStatusCode.Redirect)
                    {
                        request = CreateWebRequest(new Uri(response.Headers["Location"]), IsPost, ContentType);
                        continue;
                    }

                    throw new Exception(string.Format("unexpected HTTP response: {0}", response.StatusCode));
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
