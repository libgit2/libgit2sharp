using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibGit2Sharp.Tests
{
    public static class ProcessHelper
    {
        public static (string, int) RunProcess(string fileName, string arguments, string workingDirectory = null)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory ?? string.Empty
                }
            };

            var output = new StringBuilder();

            process.OutputDataReceived += (_, e) => output.AppendLine(e.Data);
            process.ErrorDataReceived += (_, e) => output.AppendLine(e.Data);

            process.Start();

            process.WaitForExit();

            return (output.ToString(), process.ExitCode);
        }
    }
}
