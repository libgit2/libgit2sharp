SETLOCAL

SET BASEDIR=%~dp0
SET FrameworkVersion=v4.0.30319
SET FrameworkDir=%SystemRoot%\Microsoft.NET\Framework64
SET CommitSha=%~1

"%FrameworkDir%\%FrameworkVersion%\msbuild.exe" "%BASEDIR%CI-build.msbuild" /property:CommitSha=%CommitSha%

ENDLOCAL

EXIT /B %ERRORLEVEL%