using System;
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

            switch (result)
            {
                case (int) GitErrorCode.User:
                    throw new UserCancelledException(errorMessage, (GitErrorCode)result, error.Category);

                case (int)GitErrorCode.BareRepo:
                    throw new BareRepositoryException(errorMessage, (GitErrorCode)result, error.Category);

                case (int)GitErrorCode.Exists:
                    throw new NameConflictException(errorMessage, (GitErrorCode)result, error.Category);

                case (int)GitErrorCode.InvalidSpecification:
                    throw new InvalidSpecificationException(errorMessage, (GitErrorCode)result, error.Category);

                case (int)GitErrorCode.UnmergedEntries:
                    throw new UnmergedIndexEntriesException(errorMessage, (GitErrorCode)result, error.Category);

                case (int)GitErrorCode.NonFastForward:
                    throw new NonFastForwardException(errorMessage, (GitErrorCode)result, error.Category);

                case (int)GitErrorCode.MergeConflict:
                    throw new MergeConflictException(errorMessage, (GitErrorCode)result, error.Category);

                default:
                    throw new LibGit2SharpException(errorMessage, (GitErrorCode)result, error.Category);
            }
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
