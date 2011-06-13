SET BASEDIR=%~dp0
SET SRCDIR=%BASEDIR%..\LibGit2Sharp\

REM the nuspec file needs to be next to the csproj, so copy it there during the pack operation
copy "%BASEDIR%LibGit2Sharp.nuspec" "%SRCDIR%"

pushd "%BASEDIR%"

..\Lib\NuGet\NuGet.exe pack -sym "%SRCDIR%LibGit2Sharp.csproj"

popd

del "%SRCDIR%LibGit2Sharp.nuspec"
