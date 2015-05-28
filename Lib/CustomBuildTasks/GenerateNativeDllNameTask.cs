using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CustomBuildTasks
{
    public class GenerateNativeDllNameTask : Task
    {
        public ITaskItem InputHashFile { get; set; }

        public string OutputFile { get; set; }

        public override bool Execute()
        {
            var fileName = InputHashFile.GetMetadata("FullPath");
            string hash;

            using (var sr = new StreamReader(fileName))
            {
                hash = sr.ReadLine();
            }

            var shortHash = hash.Substring(0, 7);

            var nativeDllName = @"namespace LibGit2Sharp.Core
{{
    internal static class NativeDllName
    {{
        public const string Name = ""git2-{0}"";
    }}
}}
";

            using (var sw = new StreamWriter(OutputFile))
            {
                sw.Write(nativeDllName, shortHash);
            }

            return true;
        }
    }
}
