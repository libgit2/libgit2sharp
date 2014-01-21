using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads for a <see cref="Signature"/>
    /// </summary>
    internal static class SignatureExtensions
    {
        /// <summary>
        /// If the signature is null, return the default using configuration values.
        /// </summary>
        /// <param name="signature">The signature to test</param>
        /// <param name="config">The configuration to query for default values</param>
        /// <returns>A valid <see cref="Signature"/></returns>
        public static Signature OrDefault(this Signature signature, Configuration config)
        {
            return signature ?? config.BuildSignature(DateTimeOffset.Now);
        }
    }
}
