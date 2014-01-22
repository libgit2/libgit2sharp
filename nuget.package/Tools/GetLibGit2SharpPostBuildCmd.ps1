$solutionDir = [System.IO.Path]::GetDirectoryName($dte.Solution.FullName) + "\"
$path = $installPath.Replace($solutionDir, "`$(SolutionDir)")

$NativeAssembliesDir = Join-Path $path "lib\net35\NativeBinaries"
$x86 = $(Join-Path $NativeAssembliesDir "x86\*.*")
$x64 = $(Join-Path $NativeAssembliesDir "amd64\*.*")

$LibGit2SharpPostBuildCmd = "
if not exist `"`$(TargetDir)NativeBinaries`" md `"`$(TargetDir)NativeBinaries`"
if not exist `"`$(TargetDir)NativeBinaries\x86`" md `"`$(TargetDir)NativeBinaries\x86`"
xcopy /s /y /d `"$x86`" `"`$(TargetDir)NativeBinaries\x86`"
if not exist `"`$(TargetDir)NativeBinaries\amd64`" md `"`$(TargetDir)NativeBinaries\amd64`"
xcopy /s /y /d `"$x64`" `"`$(TargetDir)NativeBinaries\amd64`""
