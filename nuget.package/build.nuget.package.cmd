SETLOCAL
SET BASEDIR=%~dp0
SET SRCDIR=%BASEDIR%..\LibGit2Sharp\
SET CommitSha=%~1

IF "%CommitSha%" == "" (
	ECHO "Please provide the Libgit2Sharp commit Sha this package is being built from."
	EXIT /B 1
)

REM the nuspec file needs to be next to the csproj, so copy it there during the pack operation
COPY "%BASEDIR%LibGit2Sharp.nuspec" "%SRCDIR%"

PUSHD "%BASEDIR%"

DEL *.nupkg

CMD /c "..\build.libgit2sharp.cmd %CommitSha%"

IF %ERRORLEVEL% NEQ 0 GOTO EXIT

"..\Lib\NuGet\NuGet.exe" Pack -Symbols "%SRCDIR%LibGit2Sharp.csproj" -Prop Configuration=Release

:EXIT
DEL "%SRCDIR%LibGit2Sharp.nuspec"

ENDLOCAL
POPD

PAUSE
EXIT /B %ERRORLEVEL%
