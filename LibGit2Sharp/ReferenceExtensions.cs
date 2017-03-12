using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helpers to a <see cref="Reference"/>.
    /// </summary>
    internal static class ReferenceExtensions
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
    }
}
