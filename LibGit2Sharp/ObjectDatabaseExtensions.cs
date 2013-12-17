using System.IO;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref = "ObjectDatabase" />.
    /// </summary>
    public static class ObjectDatabaseExtensions
    {
        /// <summary>
        /// Create a TAR archive of the given tree.
        /// </summary>
        /// <param name="odb">The object database.</param>
        /// <param name="tree">The tree.</param>
        /// <param name="archivePath">The archive path.</param>
        public static void Archive(this ObjectDatabase odb, Tree tree, string archivePath)
        {
            using (var output = new FileStream(archivePath, FileMode.Create))
            using (var archiver = new TarArchiver(output))
            {
                odb.Archive(tree, archiver);
            }
        }

        /// <summary>
        /// Create a TAR archive of the given commit.
        /// </summary>
        /// <param name="odb">The object database.</param>
        /// <param name="commit">commit.</param>
        /// <param name="archivePath">The archive path.</param>
        public static void Archive(this ObjectDatabase odb, Commit commit, string archivePath)
        {
            using (var output = new FileStream(archivePath, FileMode.Create))
            using (var archiver = new TarArchiver(output))
            {
                odb.Archive(commit, archiver);
            }
        }
    }
}
