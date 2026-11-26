using BenchmarkDotNet.Attributes;

namespace LibGit2Sharp.Benchmarks
{
    /// <summary>
    /// Every commit yielded by the walk builds an <see cref="ObjectId"/> from native memory.
    /// </summary>
    [MemoryDiagnoser]
    public class CommitWalkBenchmarks
    {
        TempRepository repo;

        [Params(1000)]
        public int CommitCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            repo = new TempRepository();

            for (var i = 0; i < CommitCount; i++)
            {
                repo.Commit("file.txt", i.ToString(), $"Commit {i}");
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            repo.Dispose();
        }

        [Benchmark]
        public int WalkCommits()
        {
            var count = 0;

            foreach (var commit in repo.Repository.Commits)
            {
                if (commit.Id != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
