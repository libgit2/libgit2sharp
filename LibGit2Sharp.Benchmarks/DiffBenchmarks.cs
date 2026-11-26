using System.Text;
using BenchmarkDotNet.Attributes;

namespace LibGit2Sharp.Benchmarks
{
    /// <summary>
    /// Diffs two versions of a file where every other line changed, so the
    /// per-line diff callbacks dominate.
    /// </summary>
    [MemoryDiagnoser]
    public class DiffBenchmarks
    {
        TempRepository repo;
        Commit oldCommit;
        Commit newCommit;
        Blob oldBlob;
        Blob newBlob;

        [Params(10000)]
        public int LineCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            repo = new TempRepository();
            oldCommit = repo.Commit("file.txt", BuildContent(false), "Old");
            newCommit = repo.Commit("file.txt", BuildContent(true), "New");
            oldBlob = (Blob)oldCommit["file.txt"].Target;
            newBlob = (Blob)newCommit["file.txt"].Target;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            repo.Dispose();
        }

        string BuildContent(bool modified)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < LineCount; i++)
            {
                if (modified && i % 2 == 0)
                {
                    builder.Append("Changed line ").Append(i).Append('\n');
                    continue;
                }

                builder.Append("Original line ").Append(i).Append('\n');
            }

            return builder.ToString();
        }

        [Benchmark]
        public int PatchBetweenTrees()
        {
            var patch = repo.Repository.Diff.Compare<Patch>(oldCommit.Tree, newCommit.Tree);
            return patch.LinesAdded;
        }

        [Benchmark]
        public int ContentChangesBetweenBlobs()
        {
            var changes = repo.Repository.Diff.Compare(oldBlob, newBlob);
            return changes.LinesAdded;
        }
    }
}
