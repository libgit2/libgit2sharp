using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Tests
{
    public class TestApp
    {
        [DllImport("kernel32")]
        private static extern IntPtr GetModuleHandle(string path);

        [DllImport("kernel32")]
        private static extern int GetModuleFileName(IntPtr handle, [Out]StringBuilder path, int size);

        static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.Error.WriteLine("Usage: <module-name> <directory>");
                return -1;
            }

            var moduleName = args[0];
            var loadFromDirectory = args[1];
            var expectedPath = Path.Combine(loadFromDirectory, (IntPtr.Size == 4) ? "x86" : "x64", moduleName + ".dll");

            GlobalSettings.NativeLibraryPath = loadFromDirectory;
            var isValid = Repository.IsValid(Path.GetTempPath());

            var capacity = ushort.MaxValue;
            var moduleHandle = GetModuleHandle(moduleName);
            var buffer = new StringBuilder(capacity);
            int actualLength = GetModuleFileName(moduleHandle, buffer, capacity);
            var actualPath = buffer.ToString(0, actualLength);

            if (expectedPath != actualPath)
            {
                Console.WriteLine(actualPath);
                return 1;
            }

            return 0;
        }
    }
}
