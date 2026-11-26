using System;
using System.IO;

namespace LibGit2Sharp.Benchmarks
{
    /// <summary>
    /// A throwaway repository on disk, deleted on dispose.
    /// </summary>
    sealed class TempRepository : IDisposable
    {
        static readonly Signature signature = new Signature("Benchmark", "benchmark@example.com", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        readonly string directory;

        public TempRepository()
        {
            directory = Path.Combine(Path.GetTempPath(), "LibGit2Sharp.Benchmarks", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Repository.Init(directory);
            Repository = new Repository(directory);
        }

        public Repository Repository { get; }

        public Commit Commit(string fileName, string content, string message)
        {
            File.WriteAllText(Path.Combine(directory, fileName), content);
            Commands.Stage(Repository, fileName);
            return Repository.Commit(message, signature, signature);
        }

        public void Dispose()
        {
            Repository.Dispose();

            // libgit2 writes object files as read-only, which blocks Directory.Delete on Windows.
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(directory, true);
        }
    }
}
