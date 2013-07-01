using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helpers to a <see cref="Reference"/>.
    /// </summary>
    public static class ReferenceExtensions
    {
        internal static bool LooksLikeLocalBranch(this string canonicalName)
        {
            return canonicalName.IsPrefixedBy(Reference.LocalBranchPrefix);
        }

        internal static bool LooksLikeRemoteTrackingBranch(this string canonicalName)
        {
            return canonicalName.IsPrefixedBy(Reference.RemoteTrackingBranchPrefix);
        }

        internal static bool LooksLikeTag(this string canonicalName)
        {
            return canonicalName.IsPrefixedBy(Reference.TagPrefix);
        }

        internal static bool LooksLikeNote(this string canonicalName)
        {
            return canonicalName.IsPrefixedBy(Reference.NotePrefix);
        }

        private static bool IsPrefixedBy(this string input, string prefix)
        {
            return input.StartsWith(prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a local branch.
        /// </summary>
        /// <param name="reference">The <see cref="Reference"/> to test.</param>
        /// <returns>true if the current <see cref="Reference"/> is a local branch, false otherwise.</returns>
        public static bool IsLocalBranch(this Reference reference)
        {
            return reference.CanonicalName.LooksLikeLocalBranch();
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a remote tracking branch.
        /// </summary>
        /// <param name="reference">The <see cref="Reference"/> to test.</param>
        /// <returns>true if the current <see cref="Reference"/> is a remote tracking branch, false otherwise.</returns>
        public static bool IsRemoteTrackingBranch(this Reference reference)
        {
            return reference.CanonicalName.LooksLikeRemoteTrackingBranch();
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a tag.
        /// </summary>
        /// <param name="reference">The <see cref="Reference"/> to test.</param>
        /// <returns>true if the current <see cref="Reference"/> is a tag, false otherwise.</returns>
        public static bool IsTag(this Reference reference)
        {
            return reference.CanonicalName.LooksLikeTag();
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a note.
        /// </summary>
        /// <param name="reference">The <see cref="Reference"/> to test.</param>
        /// <returns>true if the current <see cref="Reference"/> is a note, false otherwise.</returns>
        public static bool IsNote(this Reference reference)
        {
            return reference.CanonicalName.LooksLikeNote();
        }
    }
}
