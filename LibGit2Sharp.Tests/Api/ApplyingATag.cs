/*
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

using LibGit2Sharp.Core;
using NUnit.Framework;

namespace LibGit2Sharp.Tests.Api
{
    [TestFixture]
    public class ApplyingATag : ReadWriteRepositoryFixtureBase
    {
        private static readonly Signature _signature = new Signature("nulltoken", "emeric.fermas@gmail.com", Epoch.ToDateTimeOffset(1300557894, 60));
        private const string _tagTargetId = "e90810b8df3e80c413d903f631643c716887138d";
        private const string _tagName = "nullTAGen";
        private const string _tagMessage = "I've been tagged!";

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
        public void ShouldThrowIfPassedAExistingTagName()
        {
            using (var repo = new Repository(PathToRepository))
            {
                Assert.Throws<InvalidReferenceNameException>(() => repo.ApplyTag(_tagTargetId, "very-simple", "messaged", _signature));
            }
        }

        [Test]
        public void ShouldReturnATag()
        {
            Tag appliedTag = ApplyTag();

            AssertTag(appliedTag);
        }

        private static void AssertTag(Tag appliedTag)
        {
            Assert.IsNotNull(appliedTag);
            Assert.IsNotNullOrEmpty(appliedTag.Id);
            Assert.AreEqual(ObjectType.Tag, appliedTag.Type);
            Assert.AreEqual(_tagTargetId, appliedTag.Target.Id);
            AssertSignature(_signature, appliedTag.Tagger);
        }

        private static void AssertSignature(Signature expected, Signature current)
        {
            Assert.AreEqual(expected.Email, current.Email);
            Assert.AreEqual(expected.Name, current.Name);
            TestHelper.AssertUnixDateTimeOffset(expected.When, current.When);
        }

        [Test]
        public void ShouldReturnATagEmbeddingTheTargetGitObject()
        {
            Tag appliedTag = ApplyTag();

            AssertTargetCommit(appliedTag);
        }

        private static void AssertTargetCommit(Tag appliedTag)
        {
            var target = appliedTag.Target as Commit;
            Assert.IsNotNull(target);
            Assert.AreEqual(_tagTargetId, target.Id);
            Assert.IsNotNull(target.Author);
            Assert.IsNotNull(target.Committer);
            Assert.IsNotNull(target.Message);
        }

        [Test]
        public void ShoulReturnATagWithAKnownId()
        {
            Tag appliedTag = ApplyTag();
            Assert.AreEqual("24f6de34a108d931c6056fc4687637fe36c6bd6b", appliedTag.Id);
        }

        [Test]
        public void ShouldAllowToResolveItWithItsId()
        {
            Tag appliedTag = ApplyTag();

            Tag retrievedTag;
            using (var repo = new Repository(PathToRepository))
            {
                retrievedTag = repo.Resolve<Tag>(appliedTag.Id);
            }

            AssertTag(retrievedTag);

            Assert.AreEqual(appliedTag.Id, retrievedTag.Id);
        }

        [Test]
        public void ShouldAllowToResolveItWithItsCanonicalName()
        {
            Tag appliedTag = ApplyTag();

            Tag retrievedTag;
            using (var repo = new Repository(PathToRepository))
            {
                string canonicalName = string.Format("refs/tags/{0}", _tagName);
                retrievedTag = repo.Resolve<Tag>(canonicalName);
            }

            AssertTag(retrievedTag);

            Assert.AreEqual(appliedTag.Id, retrievedTag.Id);
        }

        private Tag ApplyTag()
        {
            Tag appliedTag;
            using (var repo = new Repository(PathToRepository))
            {
                appliedTag = repo.ApplyTag(_tagTargetId, _tagName, _tagMessage, _signature);
            }
            return appliedTag;
        }
    }
}