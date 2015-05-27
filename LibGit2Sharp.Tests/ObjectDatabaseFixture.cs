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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var oid = new ObjectId(sha);

                Assert.Equal(shouldExists, repo.ObjectDatabase.Contains(oid));
            }
        }

        [Fact]
        public void CanCreateABlobFromAFileInTheWorkingDirectory()
        {
            string path = InitNewRepository();
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus("hello.txt"));

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "hello.txt"), "I'm a new file\n");

                Blob blob = repo.ObjectDatabase.CreateBlob("hello.txt");
                Assert.NotNull(blob);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", blob.Sha);

                /* The file is unknown from the Index nor the Head ... */
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus("hello.txt"));

                /* ...however, it's indeed stored in the repository. */
                var fetchedBlob = repo.Lookup<Blob>(blob.Id);
                Assert.Equal(blob, fetchedBlob);
            }
        }

        [Fact]
        public void RetrieveObjectMetadataReturnsCorrectSizeAndTypeForBlob()
        {
            string path = InitNewRepository();

            using (var repo = new Repository(path))
            {
                Blob blob = CreateBlob(repo, "I'm a new file\n");
                Assert.NotNull(blob);

                GitObjectMetadata blobMetadata = repo.ObjectDatabase.RetrieveObjectMetadata(blob.Id);
                Assert.Equal(blobMetadata.Size, blob.Size);
                Assert.Equal(blobMetadata.Type, ObjectType.Blob);

                Blob fetchedBlob = repo.Lookup<Blob>(blob.Id);
                Assert.Equal(blobMetadata.Size, fetchedBlob.Size);
            }
        }

        [Fact]
        public void CanCreateABlobIntoTheDatabaseOfABareRepository()
        {
            string path = InitNewRepository();

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
            string path = InitNewRepository();

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

        Stream PrepareMemoryStream(int contentSize)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < contentSize; i++)
            {
                sb.Append(i % 10);
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        [Theory]
        [InlineData(34, 8)]
        [InlineData(7584, 5879)]
        [InlineData(7854, 1247)]
        [InlineData(8192, 4096)]
        [InlineData(8192, 4095)]
        [InlineData(8192, 4097)]
        public void CanCreateABlobFromAStreamWithANumberOfBytesToConsume(int contentSize, int numberOfBytesToConsume)
        {
            string path = InitNewRepository();


            using (var repo = new Repository(path))
            {
                using (var stream = PrepareMemoryStream(contentSize))
                {
                    Blob blob = repo.ObjectDatabase.CreateBlob(stream, numberOfBytesToConsume: numberOfBytesToConsume);
                    Assert.Equal(numberOfBytesToConsume, blob.Size);
                }
            }
        }

        [Theory]
        [InlineData(16, 32, null)]
        [InlineData(7854, 9785, null)]
        [InlineData(16, 32, "binary.bin")]
        [InlineData(7854, 9785, "binary.bin")]
        public void CreatingABlobFromTooShortAStreamThrows(int contentSize, int numberOfBytesToConsume, string hintpath)
        {
            string path = InitNewRepository();

            using (var repo = new Repository(path))
            {
                using (var stream = PrepareMemoryStream(contentSize))
                {
                    Assert.Throws<EndOfStreamException>(() => repo.ObjectDatabase.CreateBlob(stream, hintpath, numberOfBytesToConsume));
                }
            }
        }

        [Fact]
        public void CreatingABlobFromANonReadableStreamThrows()
        {
            string path = InitNewRepository();

            using (var repo = new Repository(path))
            using (var stream = new FileStream(
                Path.Combine(repo.Info.WorkingDirectory, "file.txt"),
                FileMode.CreateNew, FileAccess.Write))
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
            string path = SandboxBareTestRepo();
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
            string path = SandboxBareTestRepo();
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
            string path = SandboxBareTestRepo();
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
            string path = InitNewRepository();
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
            string path = SandboxBareTestRepo();
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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus("hello.txt"));
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
            string path = SandboxSubmoduleTestRepo();
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

                var commitWithSubmodule = repo.ObjectDatabase.CreateCommit(Constants.Signature, Constants.Signature, "Submodule!",
                                                                           tree, new[] { repo.Head.Tip }, false);
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var td = new TreeDefinition()
                    .Add("1/new file", "hello.txt", Mode.NonExecutableFile);

                Assert.Throws<InvalidOperationException>(() => repo.ObjectDatabase.CreateTree(td));
            }
        }

        [Fact]
        public void CreatingATreeFromIndexWithUnmergedEntriesThrows()
        {
            var path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.False(repo.Index.IsFullyMerged);

                Assert.Throws<UnmergedIndexEntriesException>(
                    () => repo.ObjectDatabase.CreateTree(repo.Index));
            }
        }

        [Fact]
        public void CanCreateATreeFromIndex()
        {
            string path = SandboxStandardTestRepo();

            using (var repo = new Repository(path))
            {
                const string expectedIndexTreeSha = "0fe0fd1943a1b63ecca36fa6bbe9bbe045f791a4";

                // The tree representing the index is not in the db.
                Assert.Null(repo.Lookup(expectedIndexTreeSha));

                var tree = repo.ObjectDatabase.CreateTree(repo.Index);
                Assert.NotNull(tree);
                Assert.Equal(expectedIndexTreeSha, tree.Id.Sha);

                // The tree representing the index is now in the db.
                tree = repo.Lookup<Tree>(expectedIndexTreeSha);
                Assert.NotNull(tree);
            }
        }

        [Fact]
        public void CanCreateACommit()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch head = repo.Head;

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                td.Add("1/2/readme", td["README"]);

                Tree tree = repo.ObjectDatabase.CreateTree(td);

                Commit commit = repo.ObjectDatabase.CreateCommit(Constants.Signature, Constants.Signature, "Ü message", tree, new[] { repo.Head.Tip }, true);

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

            string path = InitNewRepository();
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
            string path = SandboxBareTestRepo();
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.ObjectDatabase.CreateCommit(
                    Constants.Signature, Constants.Signature, message, repo.Head.Tip.Tree, Enumerable.Empty<Commit>(), false));
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(
                    () => repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, null));
                Assert.Throws<ArgumentNullException>(
                    () => repo.ObjectDatabase.CalculateHistoryDivergence(null, repo.Head.Tip));
            }
        }

        [Fact]
        public void CanShortenObjectIdentifier()
        {
            /*
             * $ echo "aabqhq" | git hash-object -t blob --stdin
             * dea509d0b3cb8ee0650f6ca210bc83f4678851ba
             *
             * $ echo "aaazvc" | git hash-object -t blob --stdin
             * dea509d097ce692e167dfc6a48a7a280cc5e877e
             */

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("core.abbrev", 4);

                Blob blob1 = CreateBlob(repo, "aabqhq\n");
                Assert.Equal("dea509d0b3cb8ee0650f6ca210bc83f4678851ba", blob1.Sha);

                Assert.Equal("dea5", repo.ObjectDatabase.ShortenObjectId(blob1));
                Assert.Equal("dea509d0b3cb", repo.ObjectDatabase.ShortenObjectId(blob1, 12));
                Assert.Equal("dea509d0b3cb8ee0650f6ca210bc83f4678851b", repo.ObjectDatabase.ShortenObjectId(blob1, 39));

                Blob blob2 = CreateBlob(repo, "aaazvc\n");
                Assert.Equal("dea509d09", repo.ObjectDatabase.ShortenObjectId(blob2));
                Assert.Equal("dea509d09", repo.ObjectDatabase.ShortenObjectId(blob2, 4));
                Assert.Equal("dea509d0b", repo.ObjectDatabase.ShortenObjectId(blob1));
                Assert.Equal("dea509d0b", repo.ObjectDatabase.ShortenObjectId(blob1, 7));

                Assert.Equal("dea509d0b3cb", repo.ObjectDatabase.ShortenObjectId(blob1, 12));
                Assert.Equal("dea509d097ce", repo.ObjectDatabase.ShortenObjectId(blob2, 12));
            }
        }

        [Fact]
        public void TestMergeIntoSelfHasNoConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Lookup<Commit>("master");

                var result = repo.ObjectDatabase.CanMergeWithoutConflict(master, master);

                Assert.True(result);
            }
        }

        [Fact]
        public void TestMergeIntoOtherUnbornBranchHasNoConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/unborn");

                Touch(repo.Info.WorkingDirectory, "README", "Yeah!\n");
                repo.Index.Clear();
                repo.Stage("README");

                repo.Commit("A new world, free of the burden of the history", Constants.Signature, Constants.Signature);

                var master = repo.Branches["master"].Tip;
                var branch = repo.Branches["unborn"].Tip;

                Assert.True(repo.ObjectDatabase.CanMergeWithoutConflict(master, branch));
            }
        }

        [Fact]
        public void TestMergeIntoOtherUnbornBranchHasConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/unborn");

                repo.Index.Replace(repo.Lookup<Commit>("conflicts"));

                repo.Commit("A conflicting world, free of the burden of the history", Constants.Signature, Constants.Signature);

                var master = repo.Branches["master"].Tip;
                var branch = repo.Branches["unborn"].Tip;

                Assert.False(repo.ObjectDatabase.CanMergeWithoutConflict(master, branch));
            }
        }

        [Fact]
        public void TestMergeIntoOtherBranchHasNoConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Lookup<Commit>("master");
                var branch = repo.Lookup<Commit>("fast_forward");

                var result = repo.ObjectDatabase.CanMergeWithoutConflict(master, branch);

                Assert.True(result);
            }
        }

        [Fact]
        public void TestMergeIntoWrongBranchHasConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Lookup<Commit>("master");
                var branch = repo.Lookup<Commit>("conflicts");

                var result = repo.ObjectDatabase.CanMergeWithoutConflict(master, branch);

                Assert.False(result);
            }
        }

        private static Blob CreateBlob(Repository repo, string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return repo.ObjectDatabase.CreateBlob(stream);
            }
        }
    }
}
