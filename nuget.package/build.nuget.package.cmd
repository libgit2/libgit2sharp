SET BASEDIR=%~dp0

pushd %BASEDIR%

..\Lib\NuGet\NuGet.exe pack ./LibGit2Sharp.nuspec

popd