using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class TemporaryCloneOfTestRepo : SelfCleaningDirectory
    {
        public TemporaryCloneOfTestRepo(string sourceDirectoryPath = Constants.TestRepoPath)
        {
            var source = new DirectoryInfo(sourceDirectoryPath);

            if (Directory.Exists(Path.Combine(sourceDirectoryPath, ".git")))
            {
                // If there is a .git subfolder, we're dealing with a non-bare repo and we have to
                // copy the working folder as well

                RepositoryPath = Path.Combine(DirectoryPath, ".git");

                DirectoryHelper.CopyFilesRecursively(source, new DirectoryInfo(DirectoryPath));
            }
            else
            {
                // It's a bare repo

                var tempRepository = new DirectoryInfo(Path.Combine(DirectoryPath, source.Name));

                RepositoryPath = tempRepository.FullName;

                DirectoryHelper.CopyFilesRecursively(source, tempRepository);
            }
        }

        public string RepositoryPath { get; private set; }
    }
}