using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefTransactionFixture : BaseFixture
    {
        public static ObjectId oid1 = new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
        public static ObjectId oid2 = new ObjectId("580c2111be43802dab11328176d94c391f1deae9");

        [Fact]
        public void CanCreateTransaction()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                using (repo.Refs.NewRefTransaction())
                {
                }
            }
        }

        #region Shared transaction tests

        [Fact]
        public void ReferenceIsNotRemovedWhenTransactionIsNotCommited()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var myRef = repo.Refs.Add("refs/heads/myref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    tx.RemoveReference(myRef);
                }

                Assert.NotNull(repo.Refs[myRef.CanonicalName]);
            }
        }

        [Fact]
        public void ReferenceIsNotModifiedWhenTransactionIsNotCommitted()
        {

        }

        [Fact]
        public void CanUpdateReferenceAfterTransactionIsAbandonded()
        {

        }

        [Fact]
        public void CanRemoveReferenceInTransaction()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var myRef = repo.Refs.Add("refs/heads/myref", oid1);

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    tx.RemoveReference(myRef);
                    tx.Commit();
                }

                Assert.Null(repo.Refs[myRef.CanonicalName]);
            }
        }

        [Fact]
        public void CanUpdateDirectReferenceInTransaction()
        {
            string path = SandboxStandardTestRepo();
            string myRefName = "refs/heads/myref";

            using (var repo = new Repository(path))
            {
                var myRef = repo.Refs.Add(myRefName, oid1);

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    tx.UpdateTarget(myRef, oid2, "updated by me");
                    tx.Commit();
                }

                var updatedRef = repo.Refs[myRefName];
                Assert.NotNull(updatedRef);
                Assert.Equal(updatedRef.TargetIdentifier, oid2.Sha);
            }
        }

        [Fact]
        public void CanUpdateSymbolicReferenceInTransaction()
        {
            string path = SandboxStandardTestRepo();
            string mySymRefName = "refs/heads/symRef";
            string refTargetName = "refs/heads/myref";
            string refTarget2Name = "refs/heads/myref2";

            using (var repo = new Repository(path))
            {
                var refTarget1 = repo.Refs.Add(refTargetName, oid1);
                var refTarget2 = repo.Refs.Add(refTarget2Name, oid2);
                var mySymRef = repo.Refs.Add(mySymRefName, refTargetName, null, true);

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(mySymRef);
                    tx.UpdateTarget(mySymRef, refTarget2, null);
                    tx.Commit();
                }

                var updatedRef = repo.Refs[mySymRefName];
                Assert.NotNull(updatedRef);
                Assert.Equal(updatedRef.TargetIdentifier, refTarget2Name);
            }
        }

        [SkippableFact(Skip = "Unsure of intended behavior")]
        public void CanCreateNewDirectReferenceInTransaction()
        {

        }

        [Fact]
        public void LockingNonExistingReferenceThrows()
        {
            string path = SandboxStandardTestRepo();
            string myRefName = "refs/heads/myref";

            using (var repo = new Repository(path))
            {
                var myRef = repo.Refs.Add(myRefName, oid1);
                repo.Refs.Remove(myRef);

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    // Should this throw (reference no longer exists...)
                    Assert.Throws<LibGit2SharpException>(
                        () => tx.LockReference(myRef));
                }
            }
        }

        [Fact]
        public void LockingAlreadyLockedReferenceThrows()
        {
            string path = SandboxStandardTestRepo();
            string myRefName = "refs/heads/myref";

            using (var repo = new Repository(path))
            {
                var myRef = repo.Refs.Add(myRefName, oid1);

                using (var tx = repo.Refs.NewRefTransaction())
                using (var tx2 = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    Assert.Throws<LibGit2SharpException>(() => tx2.LockReference(myRef));
                }
            }
        }

        #endregion
    }
}
