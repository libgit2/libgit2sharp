using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.IO;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class MergeFileFixture : BaseFixture
    {
        [Fact]
        public void CanMergeZeroLengthBytes()
        {
            byte[] bytes = Array.Empty<byte>();
            bool mergeable = Repository.MergeFile(bytes, bytes, bytes, out byte[] resultBytes, null);
            Assert.True(mergeable);
            Assert.Equal(bytes, resultBytes);
        }

        [Fact]
        public void CanMergeIdenticalBytes()
        {
            byte[] bytes = new byte[] { 1 };
            bool mergeable = Repository.MergeFile(bytes, bytes, bytes, out byte[] resultBytes, null);
            Assert.True(mergeable);
            Assert.Equal(bytes, resultBytes);
        }

        [Fact]
        public void CanMergeRemovedBytes()
        {
            byte[] bytesRemoved = new byte[] { 1 };
            byte[] bytes = new byte[] { 1, 2 };
            bool mergeable = Repository.MergeFile(bytesRemoved, bytes, bytes, out byte[] resultBytes, null);
            Assert.True(mergeable);
            Assert.Equal(bytesRemoved, resultBytes);
        }

        [Fact]
        public void BytesMergeConflict()
        {
            byte[] currentBytes = new byte[] { 1, 1 };
            byte[] baseBytes = new byte[] { 1 };
            byte[] otherBytes = new byte[] { 1, 2 };
            bool mergeable = Repository.MergeFile(currentBytes, baseBytes, otherBytes, out byte[] resultBytes, null);
            Assert.False(mergeable);
            Assert.NotNull(resultBytes);
        }

        [Fact]
        public void NullInputBytesHandledGracefully()
        {
            Assert.Throws<ArgumentNullException>(() => Repository.MergeFile(null, null, null, out byte[] resultBytes, null));
        }

        [Fact]
        public void NullInputPathGracefully()
        {
            Assert.Throws<ArgumentNullException>(() => Repository.MergeFile(null, null, null, null, null));
        }

        [Fact]
        public void EmptyStringInputPathHandledGracefully()
        {
            Assert.Throws<ArgumentException>(() => Repository.MergeFile("", "", "", "", null));
        }

        [Fact]
        public void InputDoesntExistHandledGracefully()
        {
            Assert.Throws<FileNotFoundException>(() => Repository.MergeFile("test", "test", "test", "test", null));
        }

        [Theory]
        [InlineData("line 1", "line 1", "line 1", false, null, null, null, MergeFileFavor.Normal, 0, MergeFileFlag.Default, true, "line 1")] // same file
        [InlineData("line 1\nline a", "line 1", "line 1", false, null, null, null, MergeFileFavor.Normal, 0, MergeFileFlag.Default, true, "line 1\nline a")] // current changed        
        [InlineData("line 1\nline a", "line 1", "line 1\nline b", true, "Ours", "Ancestor", "Theirs", MergeFileFavor.Normal, 0, MergeFileFlag.Default, false, "line 1\n<<<<<<< Ours\nline a\n=======\nline b\n>>>>>>> Theirs\n")] // conflict
        [InlineData("line 1\nline a", "line 1", "line 1\nline b", true, null, null, null, MergeFileFavor.Ours, 0, MergeFileFlag.Default, true, "line 1\nline a")] // ours
        [InlineData("line 1\nline a", "line 1", "line 1\nline b", true, null, null, null, MergeFileFavor.Theirs, 0, MergeFileFlag.Default, true, "line 1\nline b")] // theirs
        [InlineData("line 1\nline a", "line 1", "line 1\nline b", true, null, null, null, MergeFileFavor.Union, 0, MergeFileFlag.Default, true, "line 1\nline a\nline b")] // union
        [InlineData("line 1\nline a", "line 1", "line 1\nline b", true, "Ours", "Ancestor", "Theirs", MergeFileFavor.Normal, 3, MergeFileFlag.Default, false, "line 1\n<<< Ours\nline a\n===\nline b\n>>> Theirs\n")] // marker size        
        public void CanMergeTextFiles(string currentText,
                                      string baseText,
                                      string otherText,
                                      bool setOptions,
                                      string currentLabel,
                                      string baseLabel,
                                      string otherLabel,
                                      MergeFileFavor mergeFileFavor,
                                      short markerSize,
                                      MergeFileFlag mergeFileflags,
                                      bool expectedResult,
                                      string expectedResultText)
        {

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            Directory.CreateDirectory(scd.RootedDirectoryPath);

            string currentPath = Path.Combine(scd.RootedDirectoryPath, "current.txt");
            string basePath = Path.Combine(scd.RootedDirectoryPath, "base.txt");
            string otherPath = Path.Combine(scd.RootedDirectoryPath, "other.txt");
            string resultPath = Path.Combine(scd.RootedDirectoryPath, "result.txt");

            if (currentText != null) { File.WriteAllText(currentPath, currentText); }
            if (baseText != null) { File.WriteAllText(basePath, baseText); }
            if (otherText != null) { File.WriteAllText(otherPath, otherText); }

            MergeFileOptions mergeFileOptions = new MergeFileOptions()
            {
                OurLabel = currentLabel,
                AncestorLabel = baseLabel,
                TheirLabel = otherLabel,
                Favor = mergeFileFavor,
                MarkerSize = markerSize,
                Flags = mergeFileflags
            };

            bool result = Repository.MergeFile(currentPath, basePath, otherPath, resultPath, setOptions ? mergeFileOptions : null);
            string resultText = null;
            if (File.Exists(resultPath)) { resultText = File.ReadAllText(resultPath); }

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedResultText, resultText);
        }
    }
}
