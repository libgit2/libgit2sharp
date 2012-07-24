using System;
using System.Diagnostics;
using System.Globalization;

namespace LibGit2Sharp.Core
{
    /// <summary>
    ///   Ensure input parameters
    /// </summary>
    [DebuggerStepThrough]
    internal static class Ensure
    {
        /// <summary>
        ///   Checks an argument to ensure it isn't null.
        /// </summary>
        /// <param name = "argumentValue">The argument value to check.</param>
        /// <param name = "argumentName">The name of the argument.</param>
        public static void ArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        ///   Checks a string argument to ensure it isn't null or empty.
        /// </summary>
        /// <param name = "argumentValue">The argument value to check.</param>
        /// <param name = "argumentName">The name of the argument.</param>
        public static void ArgumentNotNullOrEmptyString(string argumentValue, string argumentName)
        {
            ArgumentNotNull(argumentValue, argumentName);

            if (argumentValue.Trim().Length == 0)
            {
                throw new ArgumentException("String cannot be empty", argumentName);
            }
        }

        /// <summary>
        ///   Check that the result of a C call was successful
        ///   <para>
        ///     This usually means that the method is expected to return 0.
        ///     In some rare cases, some methods may return negative values for errors and
        ///     positive values carrying information. Those positive values should be interpreted
        ///     as successful calls as well.
        ///   </para>
        /// </summary>
        /// <param name = "result">The result to examine.</param>
        /// <param name = "allowPositiveResult">False to only allow success when comparing against 0,
        ///   True when positive values are allowed as well.</param>
        public static void Success(int result, bool allowPositiveResult = false)
        {
            if (result == (int)GitErrorCode.Ok)
            {
                return;
            }

            if (allowPositiveResult && result > (int)GitErrorCode.Ok)
            {
                return;
            }

            var error = NativeMethods.giterr_last();
            if (error == null)
            {
                throw new LibGit2SharpException(
                    (GitErrorCode)result,
                    GitErrorCategory.Unknown,
                    "No error message has been provided by the native library");
            }

            throw new LibGit2SharpException(
                (GitErrorCode)result,
                error.Category,
                Utf8Marshaler.FromNative(error.Message));
        }

        /// <summary>
        ///   Checks an argument by applying provided checker.
        /// </summary>
        /// <param name = "argumentValue">The argument value to check.</param>
        /// <param name = "checker">The predicate which has to be satisfied</param>
        /// <param name = "argumentName">The name of the argument.</param>
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
