using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class BaseFixture
    {
        static BaseFixture()
        {
            // Do the set up in the static ctor so it only happens once
            SetUpTestEnvironment();
        }

        private static void SetUpTestEnvironment()
        {
            var source = new DirectoryInfo(@"../../../Resources");
            var target = new DirectoryInfo(@"Resources");

            if (target.Exists)
            {
                target.Delete(recursive: true);
            }

            DirectoryHelper.CopyFilesRecursively(source, target);

            // The test repo under source control has its .git folder renamed to dot_git to avoid confusing git,
            // so we need to rename it back to .git in our copy under the target folder

            string tempDotGit = Path.Combine(Constants.StandardTestRepoWorkingDirPath, "dot_git");
            Directory.Move(tempDotGit, Constants.StandardTestRepoPath);
        }

        protected void CreateCorruptedDeadBeefHead(string repoPath)
        {
            const string deadbeef = "deadbeef";
            string headPath = string.Format("{0}refs/heads/{1}", repoPath, deadbeef);
            File.WriteAllText(headPath, string.Format("{0}{0}{0}{0}{0}\n", deadbeef));
        }
    }
}
