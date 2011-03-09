/*
 * The MIT License
 *
 * Copyright (c) 2011 Emeric Fermas
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
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ApplyingATag : ReadWriteRepositoryFixtureBase
    {
        private static readonly Signature _signature = new Signature("me", "me@me.me", DateTimeOffset.Now);

        [Test]
        public void ShouldThrowIfPassedANonExistingTarget()
        {
            const string invalidTargetId = "deadbeef1b46c854b31185ea97743be6a8774479";

            using (var repo = new Repository(PathToRepository))
            {
                Assert.Throws<ObjectNotFoundException>(() => repo.ApplyTag(invalidTargetId, "tagged", "messaged", _signature));
            }
        }

        [Test]
        public void ShouldReturnATag()
        {
            const string targetId = "8496071c1b46c854b31185ea97743be6a8774479";

            Tag appliedTag;

            const string tagName = "tagged";
            const string tagMessage = "messaged";

            using (var repo = new Repository(PathToRepository))
            {
                appliedTag = repo.ApplyTag(targetId, tagName, tagMessage, _signature);
            }

            Assert.IsNotNull(appliedTag);
            Assert.IsNotNullOrEmpty(appliedTag.Id);
            Assert.AreEqual(ObjectType.Tag, appliedTag.Type);
            Assert.AreEqual(targetId, appliedTag.Target.Id);
            AssertSignature(_signature, appliedTag.Tagger);
        }

        private static void AssertSignature(Signature expected, Signature current)
        {
            Assert.AreEqual(expected.Email, current.Email);
            Assert.AreEqual(expected.Name, current.Name);
            Assert.AreEqual(expected.When.ToGitDate(), current.When.ToGitDate());
        }

        [Test]
        public void ShouldReturnATagEmbeddingTheTargetGitObject()
        {
            Assert.Ignore();
        }

        [Test]
        public void ShouldWork() // TODO: Split into different tests (returnATag, PersistTheObject, MultipleApplies, ...)
        {
            const string targetId = "8496071c1b46c854b31185ea97743be6a8774479";

            Tag appliedTag;
            using (var repo = new Repository(PathToRepository))
            {
                appliedTag = repo.ApplyTag(targetId, "tagged", "messaged", _signature);
            }

            var target = appliedTag.Target as Commit;
            Assert.IsNotNull(target);

            Assert.IsNotNull(target.Author);
            Assert.IsNotNull(target.Committer);
            Assert.IsNotNull(target.Message);

            Tag retrievedTag;
            using (var repo = new Repository(PathToRepository))
            {
                retrievedTag = repo.Resolve<Tag>(appliedTag.Id);
            }

            var target2 = retrievedTag.Target as Commit;
            Assert.IsNotNull(target2);

            Assert.IsNotNull(target2.Author);
            Assert.IsNotNull(target2.Committer);
            Assert.IsNotNull(target2.Message);


            Assert.AreEqual(appliedTag.Id, retrievedTag.Id);
            // TODO: Finalize comparison

            //
        }
    }
}