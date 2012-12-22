<#
.SYNOPSIS
	Builds a version of libgit2 and copies it to Lib/NativeBinaries.
.PARAMETER sha
	Desired libgit2 version. This is run through `git rev-parse`, so branch names are okay too.
.PARAMETER vs
	Version of Visual Studio project files to generate. Cmake supports "10" (default) and "11".
.PARAMETER test
	If set, run the libgit2 tests on the desired version.
.PARAMETER debug
	If set, build the "Debug" configuration of libgit2, rather than "RelWithDebInfo" (default).
#>

Param(
	[string]$sha = 'HEAD',
	[string]$vs = '10',
	[switch]$test,
	[switch]$debug
)

Set-StrictMode -Version Latest

$self = Split-Path -Leaf $MyInvocation.MyCommand.Path
$libgit2sharpDirectory = Split-Path $MyInvocation.MyCommand.Path
$libgit2Directory = Join-Path $libgit2sharpDirectory "libgit2"
$x86Directory = Join-Path $libgit2sharpDirectory "Lib\NativeBinaries\x86"
$x64Directory = Join-Path $libgit2sharpDirectory "Lib\NativeBinaries\amd64"

$build_clar = 'OFF'
if ($test.IsPresent) { $build_clar = 'ON' }
$configuration = "RelWithDebInfo"
if ($debug.IsPresent) { $configuration = "Debug" }

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
        Write-Host -ForegroundColor yellow $output
        $error += ". See output above."
    }
    Throw $error
}

function Find-CMake {
    # Look for cmake.exe in $Env:PATH.
    $cmake = @(Get-Command cmake.exe)[0] 2>$null
    if ($cmake) {
        $cmake = $cmake.Definition
    } else {
        # Look for the highest-versioned cmake.exe in its default location.
        $cmake = @(Resolve-Path (Join-Path ${Env:ProgramFiles(x86)} "CMake *\bin\cmake.exe"))
        if ($cmake) {
            $cmake = $cmake[-1].Path
        }
    }
    if (!$cmake) {
        throw "Error: Can't find cmake.exe"
    }
    $cmake
}

function Find-Git {
	$git = @(Get-Command git)[0] 2>$null
	if ($git) {
		$git = $git.Definition
        Write-Host -ForegroundColor Gray "Using git: $git"
		& $git --version | write-host -ForegroundColor Gray
		return $git
	}
	throw "Error: Can't find git"
}

Push-Location $libgit2Directory

& {
	trap {
		Pop-Location
		break
	}

	$cmake = Find-CMake
	$ctest = Join-Path (Split-Path -Parent $cmake) "ctest.exe"
	$git = Find-Git

	Write-Output "Fetching..."
	Run-Command -Quiet { & $git fetch }

	Write-Output "Verifying $sha..."
	$sha = & $git rev-parse $sha
	if ($LASTEXITCODE -ne 0) {
		write-host -foregroundcolor red "Error: invalid SHA. USAGE: $self <SHA>"
		popd
		break
	}

	Write-Output "Checking out $sha..."
	Run-Command -Quiet -Fatal { & $git checkout $sha }

	Write-Output "Building 32-bit..."
	Run-Command -Quiet { & remove-item build -recurse -force }
	Run-Command -Quiet { & mkdir build }
	cd build
	Run-Command -Quiet -Fatal { & $cmake -G "Visual Studio $vs" -D THREADSAFE=ON -D "BUILD_CLAR=$build_clar" .. }
	Run-Command -Quiet -Fatal { & $cmake --build . --config $configuration }
	if ($test.IsPresent) { Run-Command -Quiet -Fatal { & $ctest -V . } }
	cd $configuration
	Run-Command -Quiet { & rm *.exp }
	Run-Command -Quiet -Fatal { & copy -fo * $x86Directory }

	Write-Output "Building 64-bit..."
	cd ..
	Run-Command -Quiet { & mkdir build64 }
	cd build64
	Run-Command -Quiet -Fatal { & $cmake -G "Visual Studio $vs Win64" -D THREADSAFE=ON -D "BUILD_CLAR=$build_clar" ../.. }
	Run-Command -Quiet -Fatal { & $cmake --build . --config $configuration }
	if ($test.IsPresent) { Run-Command -Quiet -Fatal { & $ctest -V . } }
	cd $configuration
	Run-Command -Quiet { & rm *.exp }
	Run-Command -Quiet -Fatal { & copy -fo * $x64Directory }

	Write-Output "Done!"
	pop-location
	sc -Encoding UTF8 libgit2sharp\libgit2_hash.txt $sha
}
exit
