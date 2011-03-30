/*
 * The MIT License
 *
 * Copyright (c) 2011 Andrius Bentkus
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
    public static class GitError
    {
        private static readonly IDictionary<git_result, Func<GitException>> _exceptionsMapper;

        static GitError()
        {
            _exceptionsMapper = BuildMapper();
        }

        private static IDictionary<git_result, Func<GitException>> BuildMapper()
        {
            var mapper = new Dictionary<git_result, Func<GitException>>();
            mapper.Add(git_result.GIT_ENOTAREPO, () => new NotAValidRepositoryException());
            mapper.Add(git_result.GIT_ENOTFOUND, () => new ObjectNotFoundException());
            mapper.Add(git_result.GIT_EINVALIDTYPE, () => new InvalidTypeException());
            mapper.Add(git_result.GIT_EINVALIDREFNAME, () => new InvalidReferenceNameException());

            return mapper;
        }

        unsafe internal static string Message(int error)
        {
            if (error < 0)
                return string.Empty;
                //TODO: Revert to code below once git_strerror() gets properly exposed on Windows
                //return new string(NativeMethods.git_strerror(error));
    
            return null;
        }
    
        public static void Check(int error)
        {
            if (error == (int)git_result.GIT_SUCCESS)
            {
                return;
            }

            Func<GitException> exceptionBuilder;
            if (Enum.IsDefined(typeof (git_result), error) && _exceptionsMapper.TryGetValue((git_result)error, out exceptionBuilder))
            {
                throw exceptionBuilder();
            }

            throw new GitInternalException(error);
        }
    }
}
