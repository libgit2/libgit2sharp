using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Structure for holding a signature extracted from a commit or a tag
    /// </summary>
    public struct SignatureInfo
    {
        /// <summary>
        /// The signature data, PGP/GPG or otherwise.
        /// </summary>
        public string Signature;
        /// <summary>
        /// The data which was signed. The object contents without the signature part.
        /// </summary>
        public string SignedData;
    }
}

