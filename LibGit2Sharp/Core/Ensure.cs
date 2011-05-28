using System;
using System.Globalization;

namespace LibGit2Sharp.Core
{
    /// <summary>
    ///   Ensure input parameters
    /// </summary>
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
        /// </summary>
        /// <param name = "result">The result.</param>
        public static void Success(int result)
        {
            if (result == (int) GitErrorCode.GIT_SUCCESS)
            {
                return;
            }

            string errorMessage = "An error occured"; // NativeMethods.git_lasterror().MarshallAsString();

            throw new ApplicationException(
                String.Format(CultureInfo.InvariantCulture, "An error was raised by libgit2. Error code = {0} ({1}).{2}{3}", Enum.GetName(typeof(GitErrorCode), result), result, Environment.NewLine, errorMessage));
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
    }
}