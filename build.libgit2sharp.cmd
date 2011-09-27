SETLOCAL

SET BASEDIR=%~dp0
SET FrameworkVersion=v4.0.30319
SET FrameworkDir=%SystemRoot%\Microsoft.NET\Framework

"%FrameworkDir%\%FrameworkVersion%\msbuild.exe" "%BASEDIR%CI-build.msbuild"

ENDLOCAL

EXIT /B %ERRORLEVEL%