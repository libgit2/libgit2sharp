<#
.SYNOPSIS
    Builds libgit2 and sticks it in Lib/NativeBinaries/x86
.PARAMETER NoClar
    By default, we build and run the Clar-based test suite. Pass -NoClar to disable this.
#>

Param(
    [switch]
    $NoClar
)

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
    $clarOption = "ON"
    if ($NoClar) {
        $clarOption = "OFF"
    }

    Run-Command -Quiet -Fatal { cmake -D BUILD_CLAR=$clarOption -D THREADSAFE=ON -D CMAKE_BUILD_TYPE=$configuration $libgit2Directory }
    Run-Command -Quiet -Fatal { cmake --build . --config $configuration }
}

function Test-Libgit2 {
    # FIXME: We should probably run libgit2_test.exe here too, but it currently
    # doesn't pass reliably.
    Run-Command -Quiet -Fatal { & $configuration\libgit2_clar.exe }
}

function Create-TempDirectory {
    $path = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item $path -Type directory -Force
}

$tempDirectory = Create-TempDirectory
Push-Location $tempDirectory

Write-Output "Building libgit2..."
Build-Libgit2

if (!$NoClar) {
    Write-Output "Testing libgit2..."
    Test-Libgit2
}

Copy-Item $configuration\git2.dll,$configuration\git2.pdb -Destination $x86Directory

Pop-Location
Remove-Item $tempDirectory -Recurse

Write-Output "Copied git2.dll and git2.pdb to $x86Directory"
