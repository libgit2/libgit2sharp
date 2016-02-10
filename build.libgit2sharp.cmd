@ECHO OFF

REM Sample usages:
REM
REM  Building and running tests
REM  - build.libgit2sharp.cmd
REM
REM  Building, running tests and embedding the libgit2sharp commit sha
REM  - build.libgit2sharp.cmd "6a6eb81272876fd63555165beef44de2aaa78a14"
REM
REM  Building and identifying potential leaks while running tests
REM  - build.libgit2sharp.cmd "unknown" "LEAKS_IDENTIFYING"


SETLOCAL

SET BASEDIR=%~dp0
SET FrameworkVersion=v4.0.30319
SET FrameworkDir=%SystemRoot%\Microsoft.NET\Framework

if exist "%SystemRoot%\Microsoft.NET\Framework64" (
  SET FrameworkDir=%SystemRoot%\Microsoft.NET\Framework64
)

ECHO ON

SET CommitSha=%~1
SET ExtraDefine=%~2

"%BASEDIR%Lib/NuGet/NuGet.exe" restore "%BASEDIR%LibGit2Sharp.sln"
"%FrameworkDir%\%FrameworkVersion%\msbuild.exe" "%BASEDIR%CI\build.msbuild" /property:CommitSha=%CommitSha% /property:ExtraDefine="%ExtraDefine%"

ENDLOCAL

EXIT /B %ERRORLEVEL%
