using System.Globalization;

namespace LibGit2Sharp.Core
{
    internal static class GitObjectExtensions
    {
        public static Commit DereferenceToCommit(this IGitObject gitObject,  string identifier, bool throwsIfCanNotBeDereferencedToACommit)
        {
            if (gitObject == null && !throwsIfCanNotBeDereferencedToACommit)
            {
                return null;
            }

            if (gitObject is Commit)
            {
                return (Commit)gitObject;
            }

            if (gitObject is TagAnnotation)
            {
                var target = ((TagAnnotation)gitObject).Target;    
                return target.DereferenceToCommit(identifier, throwsIfCanNotBeDereferencedToACommit);
            }

            if (!throwsIfCanNotBeDereferencedToACommit && (gitObject is Blob || gitObject is Tree))
            {
                return null;
            }

            throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture,
                                                     "The Git object pointed at by '{0}' can not be dereferenced to a commit.",
                                                     identifier));
        }
    }
}
