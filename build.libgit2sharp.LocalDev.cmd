@ECHO OFF

REM This is the "LocalDev" version of the build scripts. It references
REM locally-built versions of LibGit2. (The normal scripts use a LibGit2
REM NuGet package.)
REM
REM See .\LocalDev\README.txt for full details.
REM
REM Command line usage is identical to the normal script. See it for details.


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
"%FrameworkDir%\%FrameworkVersion%\msbuild.exe" "%BASEDIR%CI\build.LocalDev.msbuild" /property:CommitSha=%CommitSha% /property:ExtraDefine="%ExtraDefine%" /property:Configuration=Debug

ENDLOCAL

EXIT /B %ERRORLEVEL%
