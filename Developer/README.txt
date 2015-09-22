This file describes how to build and test LibGit2Sharp using a locally
build version of LibGit2 (rather than a NuGet-based version). The goal
is to facilitate development across all 3 layers of a LibGit2Sharp-based
application.

In order to minimize the changes to LibGit2Sharp, I've created stub
files and some slightly modified versions of .sln and .csproj files
to maintain the basic build structure of LibGit2Sharp.

LibGit2 DLL/PDB files are copied over from the local peer repo and
named "git2-developer" rather than using a hash value.


################################################################
#### Clone both repos as peers and checkout whatever commit you need.

cd <wherever>
git clone https://github.com/libgit2/libgit2.git      libgit2
git clone https://github.com/libgit2/libgit2sharp.git libgit2sharp

################################################################
#### Build 32-bit and/or 64-bit debug version of LibGit2 into 
#### libgit2/build and libgit2/build64.

cd libgit2
mkdir build
cd build
cmake -G "Visual Studio 14 2015" -DSTDCALL=ON ..
msbuild /m libgit2.sln

cd ..
mkdir build64
cd build64
cmake -G "Visual Studio 14 2015 Win64" -DSTDCALL=ON ..
msbuild /m libgit2.sln

################################################################
#### Build LibGit2Sharp and run the unit tests using the above
#### local version of LibGit2.
#### [1] This will run NuGet on the .sln to fetch all required packages.
####     Per https://docs.nuget.org/consume/command-line-reference
####     (1.1) The .sln explicitly causes .\nuget\packages.config to be loaded.
####     (1.2) And $(ProjectDir)\packages.config is loaded for each referenced
####           project.  (So both LibGit2Sharp and LibGit2Sharp.Tests.)
####     TODO This causes the LibGit2Sharp.NativeBinaries package to be
####          downloaded. However, we do not need/use it for developer mode.
####          See if NuGet has a conditional syntax to make this clear.
####
#### [2] Uses a variant of the CI build script to build LibGit2Sharp and
####     run the unit tests.
####     (2.1) I'm forcing Debug mode on LibGit2Sharp, since that's the whole
####           point of this exercise.
####     (2.2) The build process will copy the local 32/64-bit debug DLL and
####           PDB files from the peer LibGit2 during test runs.

build.Developer.cmd




Running a unit test as a 64-bit process:
https://msdn.microsoft.com/en-us/library/ee782531.aspx
