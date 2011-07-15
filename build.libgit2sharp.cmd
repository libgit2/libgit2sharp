SET BASEDIR=%~dp0
set FrameworkVersion=v4.0.30319
set FrameworkDir=%SystemRoot%\Microsoft.NET\Framework

"%FrameworkDir%\%FrameworkVersion%\msbuild.exe" "%BASEDIR%CI-build.msbuild"

exit /B %ERRORLEVEL%