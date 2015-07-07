using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibGit2Sharp.Tests.TestHelpers
{
    class FileExportFilter : Filter
    {
        public static int CleanCalledCount = 0;
        public static int CompleteCalledCount = 0;
        public static int SmudgeCalledCount = 0;
        public static readonly HashSet<string> FilesFiltered = new HashSet<string>();

        public static void Initialize()
        {
            CleanCalledCount = 0;
            CompleteCalledCount = 0;
            SmudgeCalledCount = 0;
            FilesFiltered.Clear();
        }

        protected override void Create(string root, string path, FilterMode mode, string verb)
        {
            string cachePath = GetCachePath(root, path);

            switch (mode)
            {
                case FilterMode.Clean:
                    {
                        using (File.Create(cachePath)) { }
                    }
                    break;
            }
        }

        protected override void Apply(string root, string path, Stream input, Stream output, FilterMode mode, string verb)
        {
            string cachePath = GetCachePath(root, path);

            switch (mode)
            {
                case FilterMode.Clean:
                    {
                        CleanCalledCount++;

                        using (var file = File.Open(cachePath, FileMode.Append, FileAccess.Write, FileShare.None))
                        {
                            input.CopyTo(file);
                        }
                    }
                    break;

                case FilterMode.Smudge:
                    {
                        SmudgeCalledCount++;

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
                    }
                    break;
            }
        }

        protected override void Complete(string root, string path, Stream output, FilterMode mode, string verb)
        {
            CompleteCalledCount++;

            string cachePath = GetCachePath(root, path);

            switch (mode)
            {
                case FilterMode.Clean:
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(path);
                        output.Write(bytes, 0, bytes.Length);
                        FilesFiltered.Add(path);
                    }
                    break;

                case FilterMode.Smudge:
                    {
                        if (File.Exists(cachePath))
                        {
                            using (var file = File.Open(cachePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
                            {
                                file.CopyTo(output);
                            }
                        }
                    }
                    break;
            }
        }

        private static string GetCachePath(string root, string path)
        {
            string filename = Path.GetFileName(path);
            return Path.Combine(root, ".git", filename);
        }
    }
}
