using System.Linq;
using BenchmarkDotNet.Attributes;

namespace LibGit2Sharp.Benchmarks
{
    /// <summary>
    /// Every lookup passes the id to libgit2 as a ref GitOid.
    /// </summary>
    [MemoryDiagnoser]
    public class LookupBenchmarks
    {
        TempRepository repo;
        ObjectId[] ids;

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

            ids = repo.Repository.Commits.Select(c => c.Id).ToArray();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            repo.Dispose();
        }

        [Benchmark]
        public int LookupCommits()
        {
            var count = 0;

            foreach (var id in ids)
            {
                if (repo.Repository.Lookup<Commit>(id) != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
