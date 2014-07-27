using System.IO;
using System.Text;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class OdbHelper
    {
        public static Blob CreateBlob(IRepository repo, string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return repo.ObjectDatabase.CreateBlob(stream);
            }
        }
    }
}
