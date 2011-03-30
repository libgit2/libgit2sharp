using System;

namespace LibGit2Sharp.Core
{
    /// <summary>
    ///   Ensure input parameters
    /// </summary>
    public static class Ensure
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
            if (result < 0)
            {
                throw new ApplicationException(
                    String.Format("There was an error in libgit2, but error handling sucks right now, so I can't tell you what it was. Error code = {0}", result));
            }
        }
    }
}