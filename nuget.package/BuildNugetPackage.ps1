<#
.SYNOPSIS
    Generates the NuGet packages (including the symbols).
    A clean build is performed the packaging.
.PARAMETER commitSha
    The LibGit2Sharp commit sha that contains the version of the source code being packaged.
#>

Param(
    [Parameter(Mandatory=$true)]
    [string]$commitSha,
    [scriptblock]$postBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Run-Command([scriptblock]$Command) {
    $output = ""

    $exitCode = 0
    $global:lastexitcode = 0

    & $Command

    if ($LastExitCode -ne 0) {
        $exitCode = $LastExitCode
    } elseif (!$?) {
        $exitCode = 1
    } else {
        return
    }

    $error = "``$Command`` failed"

    if ($output) {
        Write-Host -ForegroundColor "Red" $output
        $error += ". See output above."
    }

    Throw $error
}

function Clean-OutputFolder($folder) {

    If (Test-Path $folder) {
        Write-Host -ForegroundColor "Green" "Dropping `"$folder`" folder..."

        Run-Command { & Remove-Item -Recurse -Force "$folder" }

        Write-Host "Done."
    }
}

# From http://www.dougfinke.com/blog/index.php/2010/12/01/note-to-self-how-to-programmatically-get-the-msbuild-path-in-powershell/

Function Get-MSBuild {
    $lib = [System.Runtime.InteropServices.RuntimeEnvironment]
    $rtd = $lib::GetRuntimeDirectory()
    Join-Path $rtd msbuild.exe
}

#################

$root = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$projectPath = Join-Path $root "..\LibGit2Sharp"
$slnPath = Join-Path $projectPath "..\LibGit2Sharp.sln"

Remove-Item (Join-Path $projectPath "*.nupkg")

Clean-OutputFolder (Join-Path $projectPath "bin\")
Clean-OutputFolder (Join-Path $projectPath "obj\")

# The nuspec file needs to be next to the csproj, so copy it there during the pack operation
Copy-Item (Join-Path $root "LibGit2Sharp.nuspec") $projectPath

Push-Location $projectPath

try {
  Set-Content -Encoding ASCII $(Join-Path $projectPath "libgit2sharp_hash.txt") $commitSha
  Run-Command { & "$(Join-Path $projectPath "..\Lib\NuGet\Nuget.exe")" Restore "$slnPath" }
  Run-Command { & (Get-MSBuild) "$slnPath" "/verbosity:minimal" "/p:Configuration=Release" }

  If ($postBuild) {
    Write-Host -ForegroundColor "Green" "Run post build script..."
    Run-Command { & ($postBuild) }
  }

  Run-Command { & "$(Join-Path $projectPath "..\Lib\NuGet\Nuget.exe")" Pack -Prop Configuration=Release }
}
finally {
  Pop-Location
  Remove-Item (Join-Path $projectPath "LibGit2Sharp.nuspec")
  Set-Content -Encoding ASCII $(Join-Path $projectPath "libgit2sharp_hash.txt") "unknown"
}
