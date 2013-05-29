using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CustomBuildTasks
{
    public class GenerateUniqueIdentifierTask : Task
    {
        public override bool Execute()
        {
            using (FileStream fs = new FileStream(this.OutputFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
            {
                sw.WriteLine("using System;");
                sw.WriteLine();
                sw.WriteLine("namespace LibGit2Sharp.Core");
                sw.WriteLine("{");
                sw.WriteLine("  internal static class UniqueId");
                sw.WriteLine("  {");
                sw.WriteLine("    public const String UniqueIdentifier = \"" + Guid.NewGuid().ToString() + "\";");
                sw.WriteLine("  }");
                sw.WriteLine("}");
            }

            return true;
        }

        public String OutputFile
        {
            get;
            set;
        }
    }
}
