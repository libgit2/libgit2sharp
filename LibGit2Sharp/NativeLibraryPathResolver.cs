using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.PlatformAbstractions;
using SimpleJson;

namespace LibGit2Sharp
{
    static partial class NativeLibraryPathResolver
    {
        public static string GetNativeLibraryDefaultPath()
        {
            var runtimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
            var graph = BuildRuntimeGraph();

            var compatibleRuntimeIdentifiers = graph.GetCompatibleRuntimeIdentifiers(runtimeIdentifier);

            return GetNativeLibraryPath(compatibleRuntimeIdentifiers);
        }

        private static Graph BuildRuntimeGraph()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "runtime.json";

            var rids = new Dictionary<string, Runtime>();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();
                var json = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(result);

                var runtimes = (JsonObject)json["runtimes"];

                foreach (var runtime in runtimes)
                {
                    var imports = (JsonArray)((JsonObject)runtime.Value)["#import"];

                    var importedRuntimeIdentifiers = new List<string>();

                    foreach (var import in imports)
                    {
                        importedRuntimeIdentifiers.Add((string)import);
                    }

                    rids.Add(runtime.Key, new Runtime(runtime.Key, importedRuntimeIdentifiers));
                }
            }

            return new Graph(rids);
        }

        private static string GetNativeLibraryPath(List<string> runtimeIdentifiers)
        {
            foreach (var runtimeIdentifier in runtimeIdentifiers)
            {
                if (nativeLibraries.Contains(runtimeIdentifier))
                {
                    return Path.Combine(GetExecutingAssemblyDirectory(), "runtimes", runtimeIdentifier, "native");
                }
            }

            return null;
        }

        private static string GetExecutingAssemblyDirectory()
        {
            // Assembly.CodeBase is not actually a correctly formatted
            // URI.  It's merely prefixed with `file:///` and has its
            // backslashes flipped.  This is superior to EscapedCodeBase,
            // which does not correctly escape things, and ambiguates a
            // space (%20) with a literal `%20` in the path.  Sigh.
            var managedPath = Assembly.GetExecutingAssembly().CodeBase;

            if (managedPath == null)
            {
                managedPath = Assembly.GetExecutingAssembly().Location;
            }
            else if (managedPath.StartsWith("file:///"))
            {
                managedPath = managedPath.Substring(8).Replace('/', '\\');
            }
            else if (managedPath.StartsWith("file://"))
            {
                managedPath = @"\\" + managedPath.Substring(7).Replace('/', '\\');
            }

            managedPath = Path.GetDirectoryName(managedPath);

            return managedPath;
        }

        class Runtime
        {
            public string RuntimeIdentifier { get; }

            public List<string> ImportedRuntimeIdentifiers { get; }

            public Runtime(string runtimeIdentifier, List<string> importedRuntimeIdentifiers)
            {
                RuntimeIdentifier = runtimeIdentifier;
                ImportedRuntimeIdentifiers = importedRuntimeIdentifiers;
            }
        }

        class Graph
        {
            readonly Dictionary<string, Runtime> runtimes;

            public Graph(Dictionary<string, Runtime> runtimes)
            {
                this.runtimes = runtimes;
            }

            public List<string> GetCompatibleRuntimeIdentifiers(string runtimeIdentifier)
            {
                var result = new List<string>();

                if (runtimes.TryGetValue(runtimeIdentifier, out var initialRuntime))
                {
                    var queue = new Queue<Runtime>();
                    var hash = new HashSet<string>();

                    hash.Add(runtimeIdentifier);
                    queue.Enqueue(initialRuntime);

                    while (queue.Count > 0)
                    {
                        var runtime = queue.Dequeue();
                        result.Add(runtime.RuntimeIdentifier);

                        foreach (var item in runtime.ImportedRuntimeIdentifiers)
                        {
                            if (hash.Add(item))
                            {
                                queue.Enqueue(runtimes[item]);
                            }
                        }
                    }
                }
                else
                {
                    result.Add(runtimeIdentifier);
                }

                return result;
            }
        }
    }
}
