# LibGit2Sharp.Benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for LibGit2Sharp. Every benchmark reports allocations.

The diff, commit walk and lookup benchmarks build a throwaway repository under the temp folder in their setup and delete it afterwards.

Run all benchmarks on .NET 11:

```
dotnet run -c Release -f net11.0 --project LibGit2Sharp.Benchmarks -- --filter *
```

Run on .NET Framework (Windows only):

```
dotnet run -c Release -f net472 --project LibGit2Sharp.Benchmarks -- --filter *
```

Pick benchmarks with `--filter`, for example `--filter *Diff*`. For a quicker, less precise run, add `--job short`.
