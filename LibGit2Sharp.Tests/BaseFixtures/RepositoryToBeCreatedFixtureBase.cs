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
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    public class RepositoryToBeCreatedFixtureBase
    {
        private const string testRepositoriesDirectoryName = "TestRepos";
        private static readonly string TestRepositoriesDirectoryPath = RetrieveTestRepositoriesDirectory();

        protected string PathToTempDirectory { get; private set; }

        private static void DeleteDirectory(string directoryPath)
        {
            // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

            string[] files = Directory.GetFiles(directoryPath);
            string[] dirs = Directory.GetDirectories(directoryPath);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
            Directory.Delete(directoryPath, false);
        }

        private static string RetrieveAssemblyDirectory()
        {
            // From http://stackoverflow.com/questions/52797/c-how-do-i-get-the-path-of-the-assembly-the-code-is-in/283917#283917

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        private static string RetrieveTestRepositoriesDirectory()
        {
            return Path.Combine(RetrieveAssemblyDirectory(), testRepositoriesDirectoryName);
        }

        [TestFixtureSetUp]
        public virtual void Setup()
        {
            string workDirpath = Path.Combine(Path.Combine(TestRepositoriesDirectoryPath, GetType().Name), Guid.NewGuid().ToString().Substring(0, 8));

            Directory.CreateDirectory(workDirpath);

            PathToTempDirectory = workDirpath;
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
            DeleteDirectory(TestRepositoriesDirectoryPath);
        }
    }
}