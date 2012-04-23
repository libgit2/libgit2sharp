<#
.SYNOPSIS
    Builds libgit2 and sticks it in Lib/NativeBinaries/x86
#>

$libgit2sharpDirectory = Split-Path $MyInvocation.MyCommand.Path
$libgit2Directory = Join-Path $libgit2sharpDirectory "libgit2"
$x86Directory = Join-Path $libgit2sharpDirectory "Lib\NativeBinaries\x86"
$configuration = "RelWithDebInfo"

function Run-Command([scriptblock]$Command, [switch]$Fatal, [switch]$Quiet) {
    $output = ""
    if ($Quiet) {
        $output = & $Command 2>&1
    } else {
        & $Command
    }

    if (!$Fatal) {
        return
    }

    $exitCode = 0
    if ($LastExitCode -ne 0) {
        $exitCode = $LastExitCode
    } elseif (!$?) {
        $exitCode = 1
    } else {
        return
    }

    $error = "``$Command`` failed"
    if ($output) {
        Write-Output $output
        $error += ". See output above."
    }
    Throw $error
}

function Build-Libgit2 {
    Run-Command -Fatal { cmake -D BUILD_CLAR=ON -D THREADSAFE=ON -D CMAKE_BUILD_TYPE=$configuration $libgit2Directory }
    Run-Command -Fatal { cmake --build . --config $configuration }
}

function Test-Libgit2 {
    # FIXME: We should probably run libgit2_test.exe here too, but it currently
    # doesn't pass reliably.
    Run-Command -Fatal { & $configuration\libgit2_clar.exe }
}

$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
[void](New-Item $tempDirectory -Type directory -Force)
Push-Location $tempDirectory

Write-Output "Building libgit2..."
Build-Libgit2

Write-Output "Testing libgit2..."
Test-Libgit2

Copy-Item $configuration\git2.dll,$configuration\git2.pdb -Destination $x86Directory

Pop-Location
Remove-Item $tempDirectory -Recurse

Write-Host "Copied git2.dll and git2.pdb to $x86Directory" -ForegroundColor Green
