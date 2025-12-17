using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class PatchEntryChangesFixture : BaseFixture
    {
        [Fact]
        public void PatchEntryBasics()
        {
            // Init test repo
            var path = SandboxStandardTestRepoGitDir();
            string file = "numbers.txt";

            // The repo
            using (var repo = new Repository(path))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree commitTreeWithUpdatedFile = repo.Lookup<Commit>("ec9e401").Tree;

                // Create patch by diffing
                using (var patch = repo.Diff.Compare<Patch>(rootCommitTree, commitTreeWithUpdatedFile))
                {
                    PatchEntryChanges entryChanges = patch[file];
                    Assert.Equal(2, entryChanges.LinesAdded);
                    Assert.Equal(1, entryChanges.LinesDeleted);
                    Assert.Equal(187, entryChanges.Patch.Length);
                    // Smoke test
                    Assert.Equal(Mode.NonExecutableFile, entryChanges.Mode);
                    Assert.Equal(new ObjectId("4625a3628cb78970c57e23a2fe2574514ba403c7"), entryChanges.Oid);
                    Assert.Equal(ChangeKind.Modified, entryChanges.Status);
                    Assert.Equal(file, entryChanges.OldPath);
                    Assert.Equal(Mode.NonExecutableFile, entryChanges.OldMode);
                    Assert.Equal(new ObjectId("7909961ae96accd75b6813d32e0fc1d6d52ec941"), entryChanges.OldOid);
                }
            }
        }
    }
}
