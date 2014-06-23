SETLOCAL
SET BASEDIR=%~dp0
SET SRCDIR=%BASEDIR%..\libgit2\

PUSHD "%SRCDIR%"

FOR /F "delims=" %%A in ('git rev-parse --short HEAD') do call set LIBGIT2_ID=%%A

MD "%SRCDIR%\build"
MD "%SRCDIR%\build\x86"
MD "%SRCDIR%\build\amd64"

CD "%SRCDIR%\build\x86"
cmake ..\.. -G"Visual Studio 12" -DTHREADSAFE=ON -DSTDCALL=ON -DLIBGIT2_FILENAME="git2-%LIBGIT2_ID%"
cmake --build . --config RelWithDebInfo
IF %ERRORLEVEL% NEQ 0 GOTO EXIT

CD "%SRCDIR%\build\amd64"
cmake ..\.. -G"Visual Studio 12 Win64" -DTHREADSAFE=ON -DLIBGIT2_FILENAME="git2-%LIBGIT2_ID%"
cmake --build . --config RelWithDebInfo
IF %ERRORLEVEL% NEQ 0 GOTO EXIT

CD "%BASEDIR%"

DEL LibGit2Sharp-LibGit2*.nupkg

"..\Lib\NuGet\NuGet.exe" Pack -Symbols "LibGit2Sharp-LibGit2.nuspec" -Prop Configuration=Release

:EXIT
ENDLOCAL
POPD

PAUSE
EXIT /B %ERRORLEVEL%
