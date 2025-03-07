using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffBufferToBufferFixture
    {
        [Fact]
        public void ComparingTheSameBufferReturnsNoDifference()
        {
            const string text = @"
1
2
3
4
5
6";

            var buffer = Encoding.UTF8.GetBytes(text);

            var changes = Diff.Compare(buffer, buffer);

            Assert.Equal(0, changes.LinesAdded);
            Assert.Equal(0, changes.LinesDeleted);
            Assert.Equal(string.Empty, changes.Patch);
        }

        [Fact]
        public void CanCompareTwoBuffersWithADiffOfOneHunk()
        {
            var oldText = @"1
2
4
5
6
8
7".EnsureUnixLineEndings();

            var newText = @"1
2
3
4
5
6
7
8
9".EnsureUnixLineEndings();

            var oldBuffer = Encoding.UTF8.GetBytes(oldText);
            var newBuffer = Encoding.UTF8.GetBytes(newText);

            var changes = Diff.Compare(oldBuffer, newBuffer);

            Assert.False(changes.IsBinaryComparison);

            Assert.Equal(3, changes.LinesAdded);
            Assert.Equal(1, changes.LinesDeleted);

            var expected = new StringBuilder()
                .Append("@@ -1,7 +1,9 @@\n")
                .Append(" 1\n")
                .Append(" 2\n")
                .Append("+3\n")
                .Append(" 4\n")
                .Append(" 5\n")
                .Append(" 6\n")
                .Append("+7\n")
                .Append(" 8\n")
                .Append("-7\n")
                .Append("\\ No newline at end of file\n")
                .Append("+9\n")
                .Append("\\ No newline at end of file\n");

            Assert.Equal(expected.ToString(), changes.Patch);
        }

        [Fact]
        public void CanCompareTwoBuffersWithADiffOfTwoHunks()
        {
            var oldText = @"1
2
3
4
5
6
8
7
9
10
11".EnsureUnixLineEndings();

            var newText = @"2
3
4
5
6
8
7
9
10
12".EnsureUnixLineEndings();

            var oldBuffer = Encoding.UTF8.GetBytes(oldText);
            var newBuffer = Encoding.UTF8.GetBytes(newText);

            var changes = Diff.Compare(oldBuffer, newBuffer);

            Assert.False(changes.IsBinaryComparison);

            Assert.Equal(1, changes.LinesAdded);
            Assert.Equal(2, changes.LinesDeleted);

            var expected = new StringBuilder()
                .Append("@@ -1,4 +1,3 @@\n")
                .Append("-1\n")
                .Append(" 2\n")
                .Append(" 3\n")
                .Append(" 4\n")
                .Append("@@ -8,4 +7,4 @@\n")
                .Append(" 7\n")
                .Append(" 9\n")
                .Append(" 10\n")
                .Append("-11\n")
                .Append("\\ No newline at end of file\n")
                .Append("+12\n")
                .Append("\\ No newline at end of file\n");

            Assert.Equal(expected.ToString(), changes.Patch);
        }

        [Fact]
        public void CanCompareATextualBufferAgainstABinaryBuffer()
        {
            var oldText = @"1
2
3
4
5".EnsureUnixLineEndings();

            var binaryBuffer = new byte[] { 17, 16, 0, 4, 65 };


            var oldBuffer = Encoding.UTF8.GetBytes(oldText);

            var changes = Diff.Compare(oldBuffer, binaryBuffer);

            Assert.True(changes.IsBinaryComparison);

            Assert.Equal(0, changes.LinesAdded);
            Assert.Equal(0, changes.LinesDeleted);
        }

        [Fact]
        public void CanCompareABufferAgainstANullBuffer()
        {
            var oldText = @"1
2
3
4
5".EnsureUnixLineEndings();


            var oldBuffer = Encoding.UTF8.GetBytes(oldText);

            var changes = Diff.Compare(oldBuffer, null);

            Assert.Equal(0, changes.LinesAdded);
            Assert.NotEqual(0, changes.LinesDeleted);
            Assert.NotEqual(string.Empty, changes.Patch);

            changes = Diff.Compare(null, oldBuffer);

            Assert.NotEqual(0, changes.LinesAdded);
            Assert.Equal(0, changes.LinesDeleted);
            Assert.NotEqual(string.Empty, changes.Patch);
        }

        [Fact]
        public void CanCompareABufferAgainstAnEmptyBuffer()
        {
            var oldText = @"1
2
3
4
5".EnsureUnixLineEndings();


            var oldBuffer = Encoding.UTF8.GetBytes(oldText);
            var emptyBuffer = Array.Empty<byte>();

            var changes = Diff.Compare(oldBuffer, emptyBuffer);

            Assert.Equal(0, changes.LinesAdded);
            Assert.NotEqual(0, changes.LinesDeleted);
            Assert.NotEqual(string.Empty, changes.Patch);

            changes = Diff.Compare(emptyBuffer, oldBuffer);

            Assert.NotEqual(0, changes.LinesAdded);
            Assert.Equal(0, changes.LinesDeleted);
            Assert.NotEqual(string.Empty, changes.Patch);
        }

        [Fact]
        public void ComparingTwoNullBlobsReturnsAnEmptyContentChanges()
        {
            var changes = Diff.Compare(null, null);
            Assert.False(changes.IsBinaryComparison);

            Assert.Equal(0, changes.LinesAdded);
            Assert.Equal(0, changes.LinesDeleted);
        }

        [Fact]
        public void ComparingBuffersWithNoSpacesAndIndentHeuristicOptionMakesADifference()
        {
                // Based on test diff indent heuristic from:
                // https://github.com/git/git/blob/433860f3d0beb0c6f205290bd16cda413148f098/t/t4061-diff-indent.sh#L17
                var oldContent =
@"	1
	2
	a

	b
	3
	4";
                var newContent =
@"	1
	2
	a

	b
	a

	b
	3
	4";

                var oldBuffer = Encoding.UTF8.GetBytes(oldContent);
                var newBuffer = Encoding.UTF8.GetBytes(newContent);

                var noIndentHeuristicOption = new CompareOptions { IndentHeuristic = false };
                var indentHeuristicOption = new CompareOptions { IndentHeuristic = true };

                var changes0 = Diff.Compare(oldBuffer, newBuffer, noIndentHeuristicOption);
                var changes1 = Diff.Compare(oldBuffer, newBuffer, indentHeuristicOption);

                Assert.NotEqual(changes0.Patch, changes1.Patch);
                Assert.Equal(CanonicalChangedLines(changes0), CanonicalChangedLines(changes1));
        }

        [Fact]
        public void ComparingBlobsWithNoSpacesIndentHeuristicOptionMakesNoDifference()
        {
                var oldContent =
@"	1
	2
	a
	b
	3
	4";
                var newContent =
@"	1
	2
	a
	b
	a
	b
	3
	4";

                var oldBuffer = Encoding.UTF8.GetBytes(oldContent);
                var newBuffer = Encoding.UTF8.GetBytes(newContent);

                var noIndentHeuristicOption = new CompareOptions { IndentHeuristic = false };
                var indentHeuristicOption = new CompareOptions { IndentHeuristic = true };

                var changes0 = Diff.Compare(oldBuffer, newBuffer, noIndentHeuristicOption);
                var changes1 = Diff.Compare(oldBuffer, newBuffer, indentHeuristicOption);

                Assert.Equal(changes0.Patch, changes1.Patch);

        }

        [Fact]
        public void DiffSetsTheAddedAndDeletedLinesCorrectly()
        {

                var oldContent =
                @"1
2
3
4";

                var newContent =
                   @"1
2
3
5";

                var oldBuffer = Encoding.UTF8.GetBytes(oldContent);
                var newBuffer = Encoding.UTF8.GetBytes(newContent);

                ContentChanges changes = Diff.Compare(oldBuffer, newBuffer);

                Assert.Single(changes.AddedLines);
                Assert.Single(changes.DeletedLines);

                Assert.Equal("4", changes.DeletedLines.First().Content);
                Assert.Equal("5", changes.AddedLines.First().Content);

                Assert.Equal(4, changes.DeletedLines.First().LineNumber);
                Assert.Equal(4, changes.AddedLines.First().LineNumber);

        }

         static string CanonicalChangedLines(ContentChanges changes)
         {
             // Create an ordered representation of lines that have been added or removed
             return string.Join("\n", changes.Patch.Split('\n').Where(l => l.StartsWith("+") || l.StartsWith("-")).OrderBy(l => l));
         }
    }

    public static class StringExtensionMethods
    {
        public static string EnsureUnixLineEndings(this string str) => str.Replace("\r", "");
    }
}
