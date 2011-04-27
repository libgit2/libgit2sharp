using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class TemporaryCloneOfTestRepo : SelfCleaningDirectory
    {
        public TemporaryCloneOfTestRepo(string path = Constants.TestRepoPath)
            : base(BuildTempPath())
        {
            var source = new DirectoryInfo(path);
            var tempRepository = new DirectoryInfo(Path.Combine(DirectoryPath, source.Name));

            RepositoryPath = tempRepository.FullName;
            DirectoryHelper.CopyFilesRecursively(source, tempRepository);
        }

        public string RepositoryPath { get; private set; }

        private static string BuildTempPath()
        {
            return Path.Combine(Constants.TemporaryReposPath, Guid.NewGuid().ToString().Substring(0, 8));
        }
    }
}