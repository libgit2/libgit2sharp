using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Representation of .mailmap file state.
    /// </summary>
    public class Mailmap : IDisposable
    {
        private readonly Repository repo;
        private readonly MailmapHandle mailmapHandle;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Mailmap()
        { }

        internal Mailmap(Repository repo)
        {
            this.repo = repo;

            mailmapHandle = Proxy.git_mailmap_from_repository(repo.Handle);
        }

        /// <summary>
        /// Resolve a name and email to the corresponding real email.
        /// </summary>
        /// <param name="name">the name to look up</param>
        /// <param name="email">the email to look up</param>
        /// <returns>the real email</returns>
        public virtual string ResolveRealEmail(string name, string email)
        {
            Proxy.git_mailmap_resolve(out var realName, out var realEmail, mailmapHandle, name, email);

            return realEmail;
        }

        /// <summary>
        /// Resolve a name and email to the corresponding real name.
        /// </summary>
        /// <param name="name">the name to look up</param>
        /// <param name="email">the email to look up</param>
        /// <returns>the real name</returns>
        public virtual string ResolveRealName(string name, string email)
        {
            Proxy.git_mailmap_resolve(out var realName, out var realEmail, mailmapHandle, name, email);

            return realName;
        }

        /// <summary>
        /// Resolve a signature to use real names and emails with a mailmap.
        /// </summary>
        /// <param name="signature">signature to resolve</param>
        /// <returns>new signature</returns>
        public virtual unsafe Signature ResolveSignature(Signature signature)
        {
            using (var signatureHandle = Proxy.git_mailmap_resolve_signature(mailmapHandle, signature.BuildHandle()))
            {
                return new Signature(signatureHandle);
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            mailmapHandle.SafeDispose();
        }

        #endregion
    }
}
