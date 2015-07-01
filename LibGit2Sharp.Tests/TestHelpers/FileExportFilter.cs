using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibGit2Sharp.Tests.TestHelpers
{
    class FileExportFilter : Filter
    {
        public int CleanCalledCount = 0;
        public int CompleteCalledCount = 0;
        public int SmudgeCalledCount = 0;
        public readonly HashSet<string> FilesFiltered;

        private bool clean;

        public FileExportFilter(string name, IEnumerable<FilterAttributeEntry> attributes)
            : base(name, attributes)
        {
            FilesFiltered = new HashSet<string>();
        }

        protected override void Create(string path, string root, FilterMode mode)
        {
            if (mode == FilterMode.Clean)
            {
                string filename = Path.GetFileName(path);
                string cachePath = Path.Combine(root, ".git", filename);

                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }
        }

        protected override void Clean(string path, string root, Stream input, Stream output)
        {
            CleanCalledCount++;

            string filename = Path.GetFileName(path);
            string cachePath = Path.Combine(root, ".git", filename);

            using (var file = File.Exists(cachePath) ? File.Open(cachePath, FileMode.Append, FileAccess.Write, FileShare.None) : File.Create(cachePath))
            {
                input.CopyTo(file);
            }

            clean = true;
        }

        protected override void Complete(string path, string root, Stream output)
        {
            CompleteCalledCount++;

            string filename = Path.GetFileName(path);
            string cachePath = Path.Combine(root, ".git", filename);

            if (clean)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(path);
                output.Write(bytes, 0, bytes.Length);
                FilesFiltered.Add(path);
            }
            else
            {
                if (File.Exists(cachePath))
                {
                    using (var file = File.Open(cachePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
                    {
                        file.CopyTo(output);
                    }
                }
            }
        }

        protected override void Smudge(string path, string root, Stream input, Stream output)
        {
            SmudgeCalledCount++;

            string filename = Path.GetFileName(path);
            StringBuilder text = new StringBuilder();

            byte[] buffer = new byte[64 * 1024];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                string decoded = Encoding.UTF8.GetString(buffer, 0, read);
                text.Append(decoded);
            }

            if (!FilesFiltered.Contains(text.ToString()))
                throw new FileNotFoundException();

            clean = false;
        }
    }
}
