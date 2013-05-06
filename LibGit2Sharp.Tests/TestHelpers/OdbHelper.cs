using System.IO;
using System.Text;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class OdbHelper
    {
        public static Blob CreateBlob(Repository repo, string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            using (var binReader = new BinaryReader(stream))
            {
                return repo.ObjectDatabase.CreateBlob(binReader);
            }
        }
    }
}
