using System;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class NoteFixture : BaseFixture
    {
        private static readonly Signature signatureNullToken = new Signature("nulltoken", "emeric.fermas@gmail.com", DateTimeOffset.UtcNow);
        private static readonly Signature signatureYorah = new Signature("yorah", "yoram.harmelin@gmail.com", Epoch.ToDateTimeOffset(1300557894, 60));

        [Fact]
        public void RetrievingNotesFromANonExistingGitObjectYieldsNoResult()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var notes = repo.Notes[ObjectId.Zero];

                Assert.Equal(0, notes.Count());
            }
        }

        [Fact]
        public void RetrievingNotesFromAGitObjectWhichHasNoNoteYieldsNoResult()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var notes = repo.Notes[new ObjectId("4c062a6361ae6959e06292c1fa5e2822d9c96345")];

                Assert.Equal(0, notes.Count());
            }
        }

        /*
         * $ git show 4a202 --show-notes=*
         * commit 4a202b346bb0fb0db7eff3cffeb3c70babbd2045
         * Author: Scott Chacon <schacon@gmail.com>
         * Date:   Mon May 24 10:19:04 2010 -0700
         *
         *     a third commit
         *
         * Notes:
         *     Just Note, don't you understand?
         *
         * Notes (answer):
         *     Nope
         *
         * Notes (answer2):
         *     Not Nope, Note!
         */
        [Fact]
        public void CanRetrieveNotesFromAGitObject()
        {
            var expectedMessages = new [] { "Just Note, don't you understand?\n", "Nope\n", "Not Nope, Note!\n" };

            using (var repo = new Repository(BareTestRepoPath))
            {
                var notes = repo.Notes[new ObjectId("4a202b346bb0fb0db7eff3cffeb3c70babbd2045")];

                Assert.NotNull(notes);
                Assert.Equal(3, notes.Count());
                Assert.Equal(expectedMessages, notes.Select(n => n.Message));
            }
        }

        [Fact]
        public void CanGetListOfNotesNamespaces()
        {
            var expectedNamespaces = new[] { "commits", "answer", "answer2" };

            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedNamespaces, repo.Notes.Namespaces);
                Assert.Equal(repo.Notes.DefaultNamespace, repo.Notes.Namespaces.First());
            }
        }

        /*
         * $ git show 4a202b346bb0fb0db7eff3cffeb3c70babbd2045 --show-notes=*
         * commit 4a202b346bb0fb0db7eff3cffeb3c70babbd2045
         * Author: Scott Chacon <schacon@gmail.com>
         * Date:   Mon May 24 10:19:04 2010 -0700
         *
         *     a third commit
         *
         * Notes:
         *     Just Note, don't you understand?
         *
         * Notes (answer):
         *     Nope
         *
         * Notes (answer2):
         *     Not Nope, Note!
         */
        [Fact]
        public void CanAccessNotesFromACommit()
        {
            var expectedNamespaces = new[] { "Just Note, don't you understand?\n", "Nope\n", "Not Nope, Note!\n" };

            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>("4a202b346bb0fb0db7eff3cffeb3c70babbd2045");

                Assert.Equal(expectedNamespaces, commit.Notes.Select(n => n.Message));

                // Make sure that Commit.Notes is not refreshed automatically
                repo.Notes.Create(commit.Id, "I'm batman!\n", signatureNullToken, signatureYorah, "batmobile");

                Assert.Equal(expectedNamespaces, commit.Notes.Select(n => n.Message));
            }
        }

        [Fact]
        public void CanCreateANoteOnAGitObject()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>("9fd738e8f7967c078dceed8190330fc8648ee56a");
                var note = repo.Notes.Create(commit.Id, "I'm batman!\n", signatureNullToken, signatureYorah, "batmobile");

                var newNote = commit.Notes.Single();
                Assert.Equal(note, newNote);

                Assert.Equal("I'm batman!\n", newNote.Message);
                Assert.Equal("batmobile", newNote.Namespace);
            }
        }

        [Fact]
        public void CreatingANoteWhichAlreadyExistsOverwritesThePreviousNote()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>("5b5b025afb0b4c913b4c338a42934a3863bf3644");
                Assert.NotNull(commit.Notes.FirstOrDefault(x => x.Namespace == "answer"));

                repo.Notes.Create(commit.Id, "I'm batman!\n", signatureNullToken, signatureYorah, "answer");
                var note = repo.Notes[new ObjectId("5b5b025afb0b4c913b4c338a42934a3863bf3644")].FirstOrDefault(x => x.Namespace == "answer");

                Assert.NotNull(note);

                Assert.Equal("I'm batman!\n", note.Message);
                Assert.Equal("answer", note.Namespace);
            }
        }

        [Fact]
        public void CanCompareTwoUniqueNotes()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var commit = repo.Lookup<Commit>("9fd738e8f7967c078dceed8190330fc8648ee56a");

                var firstNote = repo.Notes.Create(commit.Id, "I'm batman!\n", signatureNullToken, signatureYorah, "batmobile");
                var secondNote = repo.Notes.Create(commit.Id, "I'm batman!\n", signatureNullToken, signatureYorah, "batmobile");
                Assert.Equal(firstNote, secondNote);

                var firstNoteWithAnotherNamespace = repo.Notes.Create(commit.Id, "I'm batman!\n", signatureNullToken, signatureYorah, "batmobile2");
                Assert.NotEqual(firstNote, firstNoteWithAnotherNamespace);

                var firstNoteWithAnotherMessage = repo.Notes.Create(commit.Id, "I'm ironman!\n", signatureNullToken, signatureYorah, "batmobile");
                Assert.NotEqual(firstNote, firstNoteWithAnotherMessage);

                var anotherCommit = repo.Lookup<Commit>("c47800c7266a2be04c571c04d5a6614691ea99bd");
                var firstNoteOnAnotherCommit = repo.Notes.Create(anotherCommit.Id, "I'm batman!\n", signatureNullToken, signatureYorah, "batmobile");
                Assert.NotEqual(firstNote, firstNoteOnAnotherCommit);
            }
        }
    }
}
