using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

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
        /// Checks an array argument to ensure it isn't null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentNotNullOrEmptyEnumerable<T>(IEnumerable<T> argumentValue, string argumentName)
        {
            ArgumentNotNull(argumentValue, argumentName);

            if (!argumentValue.Any())
            {
                throw new ArgumentException("Enumerable cannot be empty", argumentName);
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

            if (String.IsNullOrWhiteSpace (argumentValue))
            {
                throw new ArgumentException("String cannot be empty", argumentName);
            }
        }

        /// <summary>
        /// Checks a string argument to ensure it doesn't contain a zero byte.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentDoesNotContainZeroByte(string argumentValue, string argumentName)
        {
            if (string.IsNullOrEmpty(argumentValue))
            {
                return;
            }

            int zeroPos = -1;
            for (var i = 0; i < argumentValue.Length; i++)
            {
                if (argumentValue[i] == '\0')
                {
                    zeroPos = i;
                    break;
                }
            }

            if (zeroPos == -1)
            {
                return;
            }

            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture,
                    "Zero bytes ('\\0') are not allowed. A zero byte has been found at position {0}.", zeroPos), argumentName);
        }

        /// <summary>
        /// Checks an argument to ensure it isn't a IntPtr.Zero (aka null).
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentNotZeroIntPtr(IntPtr argumentValue, string argumentName)
        {
            if (argumentValue == IntPtr.Zero)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Checks a pointer argument to ensure it is the expected pointer value.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="expectedValue">The expected value.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentIsExpectedIntPtr(IntPtr argumentValue, IntPtr expectedValue, string argumentName)
        {
            if (argumentValue != expectedValue)
            {
                throw new ArgumentException("Unexpected IntPtr value", argumentName);
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
                    { GitErrorCode.Conflict, (m, r, c) => new CheckoutConflictException(m, r, c) },
                    { GitErrorCode.LockedFile, (m, r, c) => new LockedFileException(m, r, c) },
                    { GitErrorCode.NotFound, (m, r, c) => new NotFoundException(m, r, c) },
                    { GitErrorCode.Peel, (m, r, c) => new PeelException(m, r, c) },
                    { GitErrorCode.Auth, (m, r, c) => new AuthenticationException(m, r, c) },
                };

        private static void HandleError(int result)
        {
            string errorMessage;
            GitError error = null;
            var errHandle = NativeMethods.giterr_last();

            if (errHandle != null && !errHandle.IsInvalid)
            {
                error = errHandle.MarshalAsGitError();
            }

            if (error == null)
            {
                error = new GitError { Category = GitErrorCategory.Unknown, Message = IntPtr.Zero };
                errorMessage = "No error message has been provided by the native library";
            }
            else
            {
                errorMessage = LaxUtf8Marshaler.FromNative(error.Message);
            }

            Func<string, GitErrorCode, GitErrorCategory, LibGit2SharpException> exceptionBuilder;
            if (!GitErrorsToLibGit2SharpExceptions.TryGetValue((GitErrorCode)result, out exceptionBuilder))
            {
                exceptionBuilder = (m, r, c) => new LibGit2SharpException(m, r, c);
            }

            throw exceptionBuilder(errorMessage, (GitErrorCode)result, error.Category);
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
        /// Check that the result of a C call returns a boolean value.
        /// <para>
        ///   The native function is expected to return strictly 0 or 1.
        /// </para>
        /// </summary>
        /// <param name="result">The result to examine.</param>
        public static void BooleanResult(int result)
        {
            if (result == 0 || result == 1)
            {
                return;
            }

            HandleError(result);
        }

        /// <summary>
        /// Check that the result of a C call that returns an integer
        /// value was successful.
        /// <para>
        ///   The native function is expected to return 0 or a positive
        ///   value for success or a negative value in the case of failure.
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

        /// <summary>
        /// Checks an argument is a positive integer.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentPositiveInt32(long argumentValue, string argumentName)
        {
            if (argumentValue >= 0 && argumentValue <= uint.MaxValue)
            {
                return;
            }

            throw new ArgumentException(argumentName);
        }

        /// <summary>
        /// Check that the result of a C call that returns a non-null GitObject
        /// using the default exception builder.
        /// <para>
        ///   The native function is expected to return a valid object value.
        /// </para>
        /// </summary>
        /// <param name="gitObject">The <see cref="GitObject"/> to examine.</param>
        /// <param name="identifier">The <see cref="GitObject"/> identifier to examine.</param>
        public static void GitObjectIsNotNull(GitObject gitObject, string identifier)
        {
            if (gitObject != null)
            {
                return;
            }

            var message = string.Format(CultureInfo.InvariantCulture,
                                        "No valid git object identified by '{0}' exists in the repository.",
                                        identifier);

            if (string.Equals("HEAD", identifier, StringComparison.Ordinal))
            {
                throw new UnbornBranchException(message);
            }

            throw new NotFoundException(message);
        }
    }
}
