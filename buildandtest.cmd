@ECHO OFF

REM Sample usages:
REM
REM  Building and running tests
REM  - buildandtest.cmd
REM
REM  Building and identifying potential leaks while running tests
REM  - buildandtest.cmd "LEAKS_IDENTIFYING"

SETLOCAL

SET EXTRADEFINE=%~1

where dotnet 1>nul 2>nul
IF ERRORLEVEL 1 (
    ECHO Cannot find dotnet.exe. Run from a VS2017 Developer Prompt.
    EXIT /B 1
)

ECHO ON

SET Configuration=Release

:: Restore packages
dotnet restore "%~dp0\"
@IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

:: Build
dotnet build "%~dp0\" /v:minimal /nologo /property:ExtraDefine="%EXTRADEFINE%"
@IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

:: Run tests on Desktop and CoreCLR
"%userprofile%\.nuget\packages\xunit.runner.console\2.4.1\tools\net46\xunit.console.exe" "%~dp0bin\LibGit2Sharp.Tests\%Configuration%\net46\LibGit2Sharp.Tests.dll" -noshadow
@IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
dotnet test "%~dp0LibGit2Sharp.Tests/LibGit2Sharp.Tests.csproj" -f netcoreapp2.1 --no-restore --no-build
@IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

EXIT /B %ERRORLEVEL%
