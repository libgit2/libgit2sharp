set FrameworkVersion=v4.0.30319
set FrameworkDir=%SystemRoot%\Microsoft.NET\Framework

"%FrameworkDir%\%FrameworkVersion%\msbuild.exe" CI-build.msbuild

exit /B %ERRORLEVEL%