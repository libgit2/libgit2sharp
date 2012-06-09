using System.IO;
using System.Text;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class TemporaryCloneOfTestRepo : SelfCleaningDirectory
    {
        public TemporaryCloneOfTestRepo(IPostTestDirectoryRemover directoryRemover, string sourceDirectoryPath)
            : base(directoryRemover)
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

        public void Touch(string parent, string file, string content = null)
        {
            var parentPath = Path.Combine(RepositoryPath, parent);
            Directory.CreateDirectory(parentPath);
            var filePath = Path.Combine(parentPath, file);
            File.WriteAllText(filePath, content ?? "", Encoding.ASCII);
        }
    }
}
