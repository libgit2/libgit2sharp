using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class SignatureGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract Signature Generate();
        internal abstract SignatureSafeHandle BuildHandle();
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConstantSignatureGenerator : SignatureGenerator
    {
        private Signature signature = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signature"></param>
        public ConstantSignatureGenerator(Signature signature)
        {
            this.signature = signature;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Signature Generate()
        {
            return signature;
        }

        internal override SignatureSafeHandle BuildHandle()
        {
            return signature.BuildHandle();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CurrentTimeSignatureGenerator : SignatureGenerator
    {
        private string name;
        private string email;

        /// <summary>
        /// This generator will generate new signatures with the  the name and
        /// email from the passed in signature, but will use current time
        /// stamps.
        /// </summary>
        /// <param name="signature"></param>
        public CurrentTimeSignatureGenerator(Signature signature)
        {
            name = signature.Name;
            email = signature.Email;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Signature Generate()
        {
            using (var signatureHandle = Proxy.git_signature_now(name, email))
            {
                return new Signature(signatureHandle);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override SignatureSafeHandle BuildHandle()
        {
            return Proxy.git_signature_now(name, email);
        }
    }
}
