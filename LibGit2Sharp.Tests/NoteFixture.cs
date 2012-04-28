using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class NoteFixture : BaseFixture
    {
        //* get default note from commit
        //- get all notes (all namespaces, equivalent to --show-notes=*)
        //- indexer with namespace
        //- add a note on a commit
        //- add a note with a namespace on a commit
        //- delete a note from a commit
        //- modify a note

        /*
         * $ git log 8496071c1b46c854b31185ea97743be6a8774479
         * commit 8496071c1b46c854b31185ea97743be6a8774479
         * Author: Scott Chacon <schacon@gmail.com>
         * Date:   Sat May 8 16:13:06 2010 -0700
         *
         *     testing
         *
         * Notes:
         *     Hi, I'm Note.
         */
        [Fact]
        public void CanRetrieveANoteOnDefaultNamespaceFromACommit()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var commit = repo.Lookup<Commit>("8496071c1b46c854b31185ea97743be6a8774479");
                var note = commit.Notes.First();

                Assert.Equal("Hi, I'm Note.\n", note.Message);
            }
        }
    }
}
