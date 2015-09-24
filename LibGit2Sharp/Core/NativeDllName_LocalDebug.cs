// When the "LocalDebug" configuration is selected we want NativeMethods.cs
// to point to a locally-built version of the LibGit2 DLL/PDB files.  These
// are always named "git2.{dll,pdb,so,...}.
//
// When a normal "Debug" or "Release" configuration is selected we want
// NativeMethods.cs to point to the DLLs in the NativeBinaries NuGet
// package.  However, these are named "git2-<sha>.{dll,pdb,...}".
//
// The DllImport() statement requires a COMPILE TIME CONSTANT STRING value
// (either a string literal or a "const string" variable), so we cannot
// have an environment variable or similar scheme to dynamically choose
// the DLL to use.
//
// Therefore, we keep the existing GENERATED NativeDllName.cs as is.
// (It is built by $/Lib/CustomBuiltTasks/GenerateNativeDllNameTask.)
// The LibGit2Sharp.csproj CONDITIONALLY includes/compiles in the normal
// builds.
//
// The .csproj CONDITIONALLY includes/compiles this file when "LocalDebug"
// is set.
//
// Note when using "LocalDebug" it is up to the user to ensure that
// their PATH (or non-Windows equivalent) is set to allow the LibGit2Sharp
// initialization to find the locally-built DLLs.

namespace LibGit2Sharp.Core
{
    internal static class NativeDllName
    {
        public const string Name = "git2";
    }
}
