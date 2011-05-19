using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class TemporaryCloneOfTestRepo : SelfCleaningDirectory
    {
        public TemporaryCloneOfTestRepo(string sourceDirectoryPath = Constants.TestRepoPath)
        {
            var source = new DirectoryInfo(sourceDirectoryPath);
            var tempRepository = new DirectoryInfo(Path.Combine(DirectoryPath, source.Name));

            RepositoryPath = tempRepository.FullName;
            DirectoryHelper.CopyFilesRecursively(source, tempRepository);
        }

        public string RepositoryPath { get; private set; }
    }
}