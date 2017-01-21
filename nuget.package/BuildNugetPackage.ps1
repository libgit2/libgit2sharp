<#
.SYNOPSIS
    Generates the NuGet packages (including the symbols).
    A clean build is performed before packaging.
.PARAMETER commitSha
    The LibGit2Sharp commit sha that contains the version of the source code being packaged.
#>

Param(
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
    return "${env:ProgramFiles(x86)}\MSBuild\14.0\Bin\msbuild.exe"
}

#################

$configuration = 'release'
$root = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$OutDir = Join-Path $root "bin\$configuration"
$projectPath = Join-Path $root "..\LibGit2Sharp"
$slnPath = Join-Path $projectPath "..\LibGit2Sharp.sln"
$nuspecPath = Join-Path $root 'LibGit2Sharp.nuspec'

if (-not (Test-Path $OutDir)) {
    $null = md $OutDir
}
Remove-Item (Join-Path $OutDir "*.nupkg")

Push-Location $root

try {
  $DependencyBasePath = $null # workaround script issue in NB.GV
  $versionInfo = & "$env:userprofile\.nuget\packages\Nerdbank.GitVersioning\1.5.51\tools\Get-Version.ps1"
  $commitSha = $versionInfo.GitCommitId

  Set-Content -Encoding ASCII $(Join-Path $projectPath "libgit2sharp_hash.txt") $commitSha
  Run-Command { & "$(Join-Path $projectPath "..\Lib\NuGet\Nuget.exe")" Restore "$slnPath" }
  Run-Command { & (Get-MSBuild) "$slnPath" "/verbosity:minimal" "/p:Configuration=$configuration" "/m" }

  If ($postBuild) {
    Write-Host -ForegroundColor "Green" "Run post build script..."
    Run-Command { & ($postBuild) }
  }

  Run-Command { & "$(Join-Path $projectPath "..\Lib\NuGet\Nuget.exe")" Pack $nuspecPath -OutputDirectory $OutDir -Prop "Configuration=$configuration;GitCommitIdShort=$($versionInfo.GitCommitIdShort)" -Version "$($versionInfo.NuGetPackageVersion)" }
}
finally {
  Pop-Location
  Set-Content -Encoding ASCII $(Join-Path $projectPath "libgit2sharp_hash.txt") "unknown"
}
