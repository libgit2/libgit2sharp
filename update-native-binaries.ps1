<#
.SYNOPSIS
    Builds libgit2 and sticks it in Lib/NativeBinaries/x86
#>

$libgit2sharpDirectory = Split-Path $MyInvocation.MyCommand.Path
$libgit2Directory = Join-Path $libgit2sharpDirectory "libgit2"
$x86Directory = Join-Path $libgit2sharpDirectory "Lib\NativeBinaries\x86"
$configuration = "RelWithDebInfo"

function Build-Libgit2 {
    & cmake -D BUILD_CLAR=ON -D THREADSAFE=ON -D CMAKE_BUILD_TYPE=$configuration $libgit2Directory
    if (!$?) {
        return
    }
    & cmake --build . --config $configuration
}

function Test-Libgit2 {
    # FIXME: We should probably run libgit2_test.exe here too, but it currently
    # doesn't pass reliably.
    & $configuration\libgit2_clar.exe
}

function Run-With-Status([string]$name, [string]$expression) {
    Write-Host "$name..."
    $output = Invoke-Expression $expression
    if ($?) {
        return
    }

    Write-Host "$name failed. Output is below." -ForegroundColor Red
    Write-Host $output
    exit 1
}

$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
[void](New-Item $tempDirectory -Type directory -Force)
Push-Location $tempDirectory

Run-With-Status "Building libgit2" "Build-Libgit2"
Run-With-Status "Testing libgit2" "Test-Libgit2"

Copy-Item $configuration\git2.dll,$configuration\git2.pdb -Destination $x86Directory

Pop-Location
Remove-Item $tempDirectory -Recurse

Write-Host "Copied git2.dll and git2.pdb to $x86Directory" -ForegroundColor Green
