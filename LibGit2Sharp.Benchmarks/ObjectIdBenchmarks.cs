using System.Linq;
using BenchmarkDotNet.Attributes;

namespace LibGit2Sharp.Benchmarks
{
    [MemoryDiagnoser]
    public class ObjectIdBenchmarks
    {
        const string sha = "8496071c1b46c854b31185ea97743be6a8774479";

        readonly byte[] rawId = new ObjectId(sha).RawId.ToArray();

        [Benchmark]
        public ObjectId FromSha()
        {
            return new ObjectId(sha);
        }

        [Benchmark]
        public ObjectId TryParse()
        {
            ObjectId.TryParse(sha, out var id);
            return id;
        }

        [Benchmark]
        public ObjectId FromRawId()
        {
            return new ObjectId(rawId);
        }
    }
}
