﻿/*
* The MIT License
*
* Copyright (c) 2011 LibGit2Sharp committers
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

using System.IO;
using LibGit2Sharp.Core;
using NUnit.Framework;

namespace LibGit2Sharp.Tests.Core
{
    [TestFixture]
    public class DatabaseFixture : ReadOnlyRepositoryFixtureBase
    {
        [Test]
        public void AnExistingObjectCanBeRead()
        {
            const string objectId = "8496071c1b46c854b31185ea97743be6a8774479";
            DatabaseObject databaseObject;

            using (var repo = new LibGit2Sharp.Core.Repository(PathToRepository))
            {
                databaseObject = repo.Database.Read(objectId);
            }

            Assert.IsNotNull(databaseObject);
            Assert.AreEqual(objectId, databaseObject.ObjectId.ToString());
            Assert.AreEqual(git_otype.GIT_OBJ_COMMIT, databaseObject.Type);
            Assert.AreEqual(172, databaseObject.Length);

            
            using (var ms = new MemoryStream(databaseObject.GetData()))
            using (var sr = new StreamReader(ms))
            {
                string content = sr.ReadToEnd();
                StringAssert.StartsWith("tree ", content);
                StringAssert.EndsWith("testing\n", content);
            }

            databaseObject.Close();
        }
    }
}
