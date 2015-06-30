using System;
using System.IO;
using Microsoft.Build.Utilities;
using CustomBuildTasks;

namespace CustomeBuildTasksExe
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "UniqueIdentifierTask")
                {
                    var guidTask = new GenerateUniqueIdentifierTask();
                    guidTask.OutputFile = args.Length > 1 ? args[1] : Path.Combine(Environment.CurrentDirectory, "Core/UniqueIdentifier.cs");
                    guidTask.Execute();
                    Console.WriteLine("Generated: {0}", guidTask.OutputFile);
                }
                else if (args[0] == "NativeDllNameTask")
                {
                    string fileName;
                    fileName = args.Length < 2 ? Directory.GetFiles("../packages", "libgit2_hash.txt", SearchOption.AllDirectories)[0] : args[1];
                    var dllNameTask = new GenerateNativeDllNameTask();
                    dllNameTask.InputHashFile = new TaskItem(fileName);
                    dllNameTask.OutputFile = args.Length > 1 ? args[2] : Path.Combine(Environment.CurrentDirectory, "Core/NativeDllName.cs");
                    dllNameTask.Execute();
                    Console.WriteLine("Generated: {0}", dllNameTask.OutputFile);
                    return;
                }
                else
                {
                    Console.WriteLine("{0}: Unsupported Microsoft.Build.Utilities.Task Id supplied, no task executed.", args[0]);
                }
            }
        }
    }
}
