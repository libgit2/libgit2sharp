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
            string libgit2FileName;

            using (var sr = new StreamReader(fileName))
            {
                libgit2FileName = sr.ReadLine();
            }

            var nativeDllName = @"namespace LibGit2Sharp.Core
{{
    internal static class NativeDllName
    {{
        public const string Name = ""{0}"";
    }}
}}
";

            using (var sw = new StreamWriter(OutputFile))
            {
                sw.Write(nativeDllName, libgit2FileName);
            }

            return true;
        }
    }
}
