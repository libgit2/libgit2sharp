using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ObjectDatabaseFixture : BaseFixture
    {
        [Theory]
        [InlineData("8496071c1b46c854b31185ea97743be6a8774479", true)]
        [InlineData("1385f264afb75a56a5bec74243be9b367ba4ca08", true)]
        [InlineData("ce08fe4884650f067bd5703b6a59a8b3b3c99a09", false)]
        [InlineData("deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", false)]
        public void CanTellIfObjectsExists(string sha, bool shouldExists)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var oid = new ObjectId(sha);

                Assert.Equal(shouldExists, repo.ObjectDatabase.Contains(oid));
            }
        }

        [Fact]
        public void CanCreateABlobFromAFileInTheWorkingDirectory()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus("hello.txt"));

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "hello.txt"), "I'm a new file\n");

                Blob blob = repo.ObjectDatabase.CreateBlob("hello.txt");
                Assert.NotNull(blob);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", blob.Sha);

                /* The file is unknown from the Index nor the Head ... */
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus("hello.txt"));

                /* ...however, it's indeed stored in the repository. */
                var fetchedBlob = repo.Lookup<Blob>(blob.Id);
                Assert.Equal(blob, fetchedBlob);
            }
        }

        [Fact]
        public void CanCreateABlobIntoTheDatabaseOfABareRepository()
        {
            string path = CloneBareTestRepo();

            SelfCleaningDirectory directory = BuildSelfCleaningDirectory();

            string filepath = Touch(directory.RootedDirectoryPath, "hello.txt", "I'm a new file\n");

            using (var repo = new Repository(path))
            {
                /*
                 * $ echo "I'm a new file" | git hash-object --stdin
                 * dc53d4c6b8684c21b0b57db29da4a2afea011565
                 */
                Assert.Null(repo.Lookup<Blob>("dc53d4c6b8684c21b0b57db29da4a2afea011565"));

                Blob blob = repo.ObjectDatabase.CreateBlob(filepath);

                Assert.NotNull(blob);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", blob.Sha);
                Assert.Equal("I'm a new file\n", blob.GetContentText());

                var fetchedBlob = repo.Lookup<Blob>(blob.Id);
                Assert.Equal(blob, fetchedBlob);
            }
        }

        [Theory]
        [InlineData("321cbdf08803c744082332332838df6bd160f8f9", null)]
        [InlineData("321cbdf08803c744082332332838df6bd160f8f9", "dummy.data")]
        [InlineData("e9671e138a780833cb689753570fd10a55be84fb", "dummy.txt")]
        [InlineData("e9671e138a780833cb689753570fd10a55be84fb", "dummy.guess")]
        public void CanCreateABlobFromAStream(string expectedSha, string hintPath)
        {
            string path = CloneBareTestRepo();

            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                sb.Append("libgit2\n\r\n");
            }

            using (var repo = new Repository(path))
            {
                CreateAttributesFiles(Path.Combine(repo.Info.Path, "info"), "attributes");

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
                {
                    Blob blob = repo.ObjectDatabase.CreateBlob(stream, hintPath);
                    Assert.Equal(expectedSha, blob.Sha);
                }
            }
        }

        [Theory]
        [InlineData(16, 32)]
        [InlineData(34, 8)]
        [InlineData(7584, 5879)]
        [InlineData(7854, 1247)]
        [InlineData(7854, 9785)]
        [InlineData(8192, 4096)]
        [InlineData(8192, 4095)]
        [InlineData(8192, 4097)]
        public void CanCreateABlobFromAStreamWithANumberOfBytesToConsume(int contentSize, int numberOfBytesToConsume)
        {
            string path = CloneBareTestRepo();

            var sb = new StringBuilder();
            for (int i = 0; i < contentSize; i++)
            {
                sb.Append(i % 10);
            }

            using (var repo = new Repository(path))
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
                {
                    Blob blob = repo.ObjectDatabase.CreateBlob(stream, numberOfBytesToConsume: numberOfBytesToConsume);
                    Assert.Equal(Math.Min(numberOfBytesToConsume, contentSize), blob.Size);
                }
            }
        }

        [Fact]
        public void CreatingABlobFromANonReadableStreamThrows()
        {
            string path = CloneStandardTestRepo();

            using (var stream = new FileStream(Path.Combine(path, "file.txt"), FileMode.CreateNew, FileAccess.Write))
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.ObjectDatabase.CreateBlob(stream));
            }
        }

        private static void CreateAttributesFiles(string where, string filename)
        {
            const string attributes = "* text=auto\n*.txt text\n*.data binary\n";

            Touch(where, filename, attributes);
        }

        [Theory]
        [InlineData("README")]
        [InlineData("README AS WELL")]
        [InlineData("2/README AS WELL")]
        [InlineData("1/README AS WELL")]
        [InlineData("1")]
        public void CanCreateATreeByAlteringAnExistingOne(string targetPath)
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>(new ObjectId("a8233120f6ad708f843d861ce2b7228ec4e3dec6"));

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree)
                    .Add(targetPath, blob, Mode.NonExecutableFile);

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);
            }
        }

        [Fact]
        public void CanCreateATreeByRemovingEntriesFromExistingOne()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree)
                    .Remove("branch_file.txt")
                    .Remove("1/branch_file.txt");

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);

                Assert.Null(tree["branch_file.txt"]);
                Assert.Null(tree["1/branch_file.txt"]);
                Assert.Null(tree["1"]);

                Assert.Equal("814889a078c031f61ed08ab5fa863aea9314344d", tree.Sha);
            }
        }

        [Fact]
        public void RemovingANonExistingEntryFromATreeDefinitionHasNoSideEffect()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tree head = repo.Head.Tip.Tree;

                TreeDefinition td = TreeDefinition.From(head)
                    .Remove("123")
                    .Remove("nope")
                    .Remove("not/here")
                    .Remove("neither/in/here")
                    .Remove("README/is/a-Blob/not-a-Tree");

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);

                Assert.Equal(head, tree);
            }
        }

        [Fact]
        public void CanCreateAnEmptyTree()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var td = new TreeDefinition();

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);
                Assert.Equal("4b825dc642cb6eb9a060e54bf8d69288fbee4904", tree.Sha);
            }
        }

        [Fact]
        public void CanReplaceAnExistingTreeWithAnotherPersitedTree()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Equal(TreeEntryTargetType.Tree, td["1"].TargetType);

                TreeDefinition newTd = new TreeDefinition()
                    .Add("new/one", repo.Lookup<Blob>("a823312"), Mode.NonExecutableFile)
                    .Add("new/two", repo.Lookup<Blob>("a71586c"), Mode.NonExecutableFile)
                    .Add("new/tree", repo.Lookup<Tree>("7f76480"));

                repo.ObjectDatabase.CreateTree(newTd);

                td.Add("1", newTd["new"]);
                Assert.Equal(TreeEntryTargetType.Tree, td["1/tree"].TargetType);
            }
        }

        [Fact]
        public void CanCreateATreeContainingABlobFromAFileInTheWorkingDirectory()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus("hello.txt"));
                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "hello.txt"), "I'm a new file\n");

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree)
                    .Add("1/new file", "hello.txt", Mode.NonExecutableFile);

                TreeEntryDefinition ted = td["1/new file"];
                Assert.NotNull(ted);
                Assert.Equal(ObjectId.Zero, ted.TargetId);

                td.Add("1/2/another new file", ted);

                Tree tree = repo.ObjectDatabase.CreateTree(td);

                TreeEntry te = tree["1/new file"];
                Assert.NotNull(te.Target);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", te.Target.Sha);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", td["1/new file"].TargetId.Sha);

                te = tree["1/2/another new file"];
                Assert.NotNull(te.Target);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", te.Target.Sha);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", td["1/2/another new file"].TargetId.Sha);
            }
        }

        [Fact]
        public void CanCreateATreeContainingAGitLinkFromAnUntrackedSubmoduleInTheWorkingDirectory()
        {
            string path = CloneSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                const string submodulePath = "sm_added_and_uncommited";

                var submoduleBefore = repo.Submodules[submodulePath];
                Assert.NotNull(submoduleBefore);
                Assert.Null(submoduleBefore.HeadCommitId);

                var objectId = (ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0";

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree)
                                                  .AddGitLink(submodulePath, objectId);

                TreeEntryDefinition ted = td[submodulePath];
                Assert.NotNull(ted);
                Assert.Equal(Mode.GitLink, ted.Mode);
                Assert.Equal(objectId, ted.TargetId);
                Assert.Equal(TreeEntryTargetType.GitLink, ted.TargetType);

                Tree tree = repo.ObjectDatabase.CreateTree(td);

                TreeEntry te = tree[submodulePath];
                Assert.NotNull(te.Target);
                Assert.IsType<GitLink>(te.Target);
                Assert.Equal(objectId, te.Target.Id);

                var commitWithSubmodule = repo.ObjectDatabase.CreateCommit(Constants.Signature, Constants.Signature, "Submodule!", false,
                                                                           tree, new[] { repo.Head.Tip });
                repo.Reset(ResetMode.Soft, commitWithSubmodule);

                var submodule = repo.Submodules[submodulePath];
                Assert.NotNull(submodule);
                Assert.Equal(submodulePath, submodule.Name);
                Assert.Equal(submodulePath, submodule.Path);
                Assert.Equal(objectId, submodule.HeadCommitId);
            }
        }

        [Fact]
        public void CannotCreateATreeContainingABlobFromARelativePathAgainstABareRepository()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var td = new TreeDefinition()
                    .Add("1/new file", "hello.txt", Mode.NonExecutableFile);

                Assert.Throws<InvalidOperationException>(() => repo.ObjectDatabase.CreateTree(td));
            }
        }

        [Fact]
        public void CanCreateACommit()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch head = repo.Head;

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                td.Add("1/2/readme", td["README"]);

                Tree tree = repo.ObjectDatabase.CreateTree(td);

                Commit commit = repo.ObjectDatabase.CreateCommit(Constants.Signature, Constants.Signature, "Ü message", true, tree, new[] { repo.Head.Tip });

                Branch newHead = repo.Head;

                Assert.Equal(head, newHead);
                Assert.Equal(commit, repo.Lookup<Commit>(commit.Sha));
                Assert.Equal("Ü message\n", commit.Message);
            }
        }

        [Fact]
        public void CanCreateABinaryBlobFromAStream()
        {
            var binaryContent = new byte[] { 0, 1, 2, 3, 4, 5 };

            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                using (var stream = new MemoryStream(binaryContent))
                {
                    Blob blob = repo.ObjectDatabase.CreateBlob(stream);
                    Assert.Equal(6, blob.Size);
                    Assert.Equal(true, blob.IsBinary);
                }
            }
        }

        [Fact]
        public void CanCreateATagAnnotationPointingToAGitObject()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Head.Tip["README"].Target as Blob;
                Assert.NotNull(blob);

                TagAnnotation tag = repo.ObjectDatabase.CreateTagAnnotation(
                    "nice_blob",
                    blob,
                    Constants.Signature,
                    "I can point at blobs, too!");

                Assert.NotNull(tag);

                // The TagAnnotation is not pointed at by any reference...
                Assert.Null(repo.Tags["nice_blob"]);

                // ...but exists in the odb.
                var fetched = repo.Lookup<TagAnnotation>(tag.Id);
                Assert.Equal(tag, fetched);
            }
        }

        [Fact]
        public void CanEnumerateTheGitObjectsFromBareRepository()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                int count = 0;

                foreach (var obj in repo.ObjectDatabase)
                {
                    Assert.NotNull(obj);
                    count++;
                }

                Assert.True(count >= 1683);
            }
        }

        [Theory]
        [InlineData("\0Leading zero")]
        [InlineData("Trailing zero\0")]
        [InlineData("Zero \0inside")]
        [InlineData("\0")]
        [InlineData("\0\0\0")]
        public void CreatingACommitWithMessageContainingZeroByteThrows(string message)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.ObjectDatabase.CreateCommit(
                    Constants.Signature, Constants.Signature, message, false, repo.Head.Tip.Tree, Enumerable.Empty<Commit>()));
            }
        }

        [Theory]
        [InlineData("\0Leading zero")]
        [InlineData("Trailing zero\0")]
        [InlineData("Zero \0inside")]
        [InlineData("\0")]
        [InlineData("\0\0\0")]
        public void CreatingATagAnnotationWithNameOrMessageContainingZeroByteThrows(string input)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.ObjectDatabase.CreateTagAnnotation(
                    input, repo.Head.Tip, Constants.Signature, "message"));
                Assert.Throws<ArgumentException>(() => repo.ObjectDatabase.CreateTagAnnotation(
                    "name", repo.Head.Tip, Constants.Signature, input));
            }
        }

        [Fact]
        public void CreatingATagAnnotationWithBadParametersThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.CreateTagAnnotation(
                    null, repo.Head.Tip, Constants.Signature, "message"));
                Assert.Throws<ArgumentException>(() => repo.ObjectDatabase.CreateTagAnnotation(
                    string.Empty, repo.Head.Tip, Constants.Signature, "message"));
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.CreateTagAnnotation(
                    "name", null, Constants.Signature, "message"));
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.CreateTagAnnotation(
                    "name", repo.Head.Tip, null, "message"));
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.CreateTagAnnotation(
                    "name", repo.Head.Tip, Constants.Signature, null));
            }
        }

        [Fact]
        public void CanCreateATagAnnotationWithAnEmptyMessage()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var tagAnnotation = repo.ObjectDatabase.CreateTagAnnotation(
                    "name", repo.Head.Tip, Constants.Signature, string.Empty);

                Assert.Equal(string.Empty, tagAnnotation.Message);
            }
        }

        [Theory]
        [InlineData("c47800c", "9fd738e", "5b5b025", 1, 2)]
        [InlineData("9fd738e", "c47800c", "5b5b025", 2, 1)]
        public void CanCalculateHistoryDivergence(
            string sinceSha, string untilSha,
            string expectedAncestorSha, int? expectedAheadBy, int? expectedBehindBy)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var since = repo.Lookup<Commit>(sinceSha);
                var until = repo.Lookup<Commit>(untilSha);

                HistoryDivergence div = repo.ObjectDatabase.CalculateHistoryDivergence(since, until);

                Assert.Equal(expectedAheadBy, div.AheadBy);
                Assert.Equal(expectedBehindBy, div.BehindBy);
                Assert.Equal(expectedAncestorSha, div.CommonAncestor.Id.ToString(7));
            }
        }

        [Theory]
        [InlineData("c47800c", "41bc8c6907", 3, 2)]
        public void CanCalculateHistoryDivergenceWhenNoAncestorIsShared(
            string sinceSha, string untilSha,
            int? expectedAheadBy, int? expectedBehindBy)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var since = repo.Lookup<Commit>(sinceSha);
                var until = repo.Lookup<Commit>(untilSha);

                HistoryDivergence div = repo.ObjectDatabase.CalculateHistoryDivergence(since, until);

                Assert.Equal(expectedAheadBy, div.AheadBy);
                Assert.Equal(expectedBehindBy, div.BehindBy);
                Assert.Null(div.CommonAncestor);
            }
        }

        [Fact]
        public void CalculatingHistoryDivergenceWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(
                    () => repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, null));
                Assert.Throws<ArgumentNullException>(
                    () => repo.ObjectDatabase.CalculateHistoryDivergence(null, repo.Head.Tip));
            }
        }
    }
}
