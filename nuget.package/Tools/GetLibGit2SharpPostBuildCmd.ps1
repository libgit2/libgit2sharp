$solutionDir = [System.IO.Path]::GetDirectoryName($dte.Solution.FullName) + "\"
$path = $installPath.Replace($solutionDir, "`$(SolutionDir)")

$NativeAssembliesDir = Join-Path $path "NativeBinaries"
$x86 = $(Join-Path $NativeAssembliesDir "x86\*.*")

$LibGit2SharpPostBuildCmd = "
xcopy /s /y `"$x86`" `"`$(TargetDir)`""