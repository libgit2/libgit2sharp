#region  Copyright (c) 2011 LibGit2Sharp committers

//  The MIT License
//  
//  Copyright (c) 2011 LibGit2Sharp committers
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.

#endregion

using System;
using System.Globalization;
using LibGit2Sharp.Properties;

namespace LibGit2Sharp
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
                throw new ArgumentException(Resources.EmptyString, argumentName);
            }
        }

        /// <summary>
        ///   Check that the result of a C call was successful
        /// </summary>
        /// <param name = "result">The result.</param>
        public static void Success(int result)
        {
            if (result < 0)
                throw new ApplicationException(
                    "There was an error in libgit2, but error handling sucks right now, so I can't tell you what it was.");
        }

        /// <summary>
        ///   Checks that the type is assignable.
        /// </summary>
        /// <param name = "toType"></param>
        /// <param name = "fromType"></param>
        public static void TypeIsAssignableFromType(Type toType, Type fromType)
        {
            if (toType.IsAssignableFrom(fromType) == false)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture,
                                  Resources.TypeNotAssignable, fromType, toType));
            }
        }
    }
}