using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Ensure input parameters
    /// </summary>
    [DebuggerStepThrough]
    internal static class Ensure
    {
        /// <summary>
        /// Checks an argument to ensure it isn't null.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Checks a string argument to ensure it isn't null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentNotNullOrEmptyString(string argumentValue, string argumentName)
        {
            ArgumentNotNull(argumentValue, argumentName);

            if (argumentValue.Trim().Length == 0)
            {
                throw new ArgumentException("String cannot be empty", argumentName);
            }
        }

        private static readonly Dictionary<GitErrorCode, Func<string, GitErrorCode, GitErrorCategory, LibGit2SharpException>>
            GitErrorsToLibGit2SharpExceptions =
                new Dictionary<GitErrorCode, Func<string, GitErrorCode, GitErrorCategory, LibGit2SharpException>>
                {
                    { GitErrorCode.User, (m, r, c) => new UserCancelledException(m, r, c) },
                    { GitErrorCode.BareRepo, (m, r, c) => new BareRepositoryException(m, r, c) },
                    { GitErrorCode.Exists, (m, r, c) => new NameConflictException(m, r, c) },
                    { GitErrorCode.InvalidSpecification, (m, r, c) => new InvalidSpecificationException(m, r, c) },
                    { GitErrorCode.UnmergedEntries, (m, r, c) => new UnmergedIndexEntriesException(m, r, c) },
                    { GitErrorCode.NonFastForward, (m, r, c) => new NonFastForwardException(m, r, c) },
                    { GitErrorCode.MergeConflict, (m, r, c) => new MergeConflictException(m, r, c) },
                    { GitErrorCode.LockedFile, (m, r, c) => new LockedFileException(m, r, c) },
                };

        private static void HandleError(int result)
        {
            string errorMessage;
            GitError error = NativeMethods.giterr_last().MarshalAsGitError();

            if (error == null)
            {
                error = new GitError { Category = GitErrorCategory.Unknown, Message = IntPtr.Zero };
                errorMessage = "No error message has been provided by the native library";
            }
            else
            {
                errorMessage = Utf8Marshaler.FromNative(error.Message);
            }

            Func<string, GitErrorCode, GitErrorCategory, LibGit2SharpException> exceptionBuilder;
            if (!GitErrorsToLibGit2SharpExceptions.TryGetValue((GitErrorCode) result, out exceptionBuilder))
            {
                exceptionBuilder = (m, r, c) => new LibGit2SharpException(m, r, c);
            }

            throw exceptionBuilder(errorMessage, (GitErrorCode) result, error.Category);
        }

        /// <summary>
        /// Check that the result of a C call was successful
        /// <para>
        ///   The native function is expected to return strictly 0 for
        ///   success or a negative value in the case of failure.
        /// </para>
        /// </summary>
        /// <param name="result">The result to examine.</param>
        public static void ZeroResult(int result)
        {
            if (result == (int)GitErrorCode.Ok)
            {
                return;
            }

            HandleError(result);
        }

        /// <summary>
        /// Check that the result of a C call that returns a boolean value
        /// was successful
        /// <para>
        ///   The native function is expected to return strictly 0 for
        ///   success or a negative value in the case of failure.
        /// </para>
        /// </summary>
        /// <param name="result">The result to examine.</param>
        public static void BooleanResult(int result)
        {
            if (result == (int)GitErrorCode.Ok || result == 1)
            {
                return;
            }

            HandleError(result);
        }

        /// <summary>
        /// Check that the result of a C call that returns an integer value
        /// was successful
        /// <para>
        ///   The native function is expected to return strictly 0 for
        ///   success or a negative value in the case of failure.
        /// </para>
        /// </summary>
        /// <param name="result">The result to examine.</param>
        public static void Int32Result(int result)
        {
            if (result >= (int)GitErrorCode.Ok)
            {
                return;
            }

            HandleError(result);
        }

        /// <summary>
        /// Checks an argument by applying provided checker.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="checker">The predicate which has to be satisfied</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentConformsTo<T>(T argumentValue, Func<T, bool> checker, string argumentName)
        {
            if (checker(argumentValue))
            {
                return;
            }

            throw new ArgumentException(argumentName);
        }

        public static void GitObjectIsNotNull(GitObject gitObject, string identifier)
        {
            if (gitObject != null)
            {
                return;
            }

            throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture,
                                                     "No valid git object identified by '{0}' exists in the repository.",
                                                     identifier));
        }
    }
}
