This file describes how to build and test LibGit2Sharp using a locally
build version of LibGit2 (rather than a NuGet-based version). The goal
is to facilitate development and debugging across all 3 layers of a
LibGit2Sharp-based application.

In order to minimize the changes to LibGit2Sharp, I've created stub
files and some slightly modified versions of the msbuild files (*.sln,
*.csproj, and *.msbuild) to maintain the same basic build structure
of LibGit2Sharp. The new .csproj files import the .props file in this
directory rather than importing the file from the NuGet package.
(In "https://msdn.microsoft.com/en-us/library/92x05xfs.aspx" it is
stated that <Import> does support conditionals, but that they only work
from the command line, not from Visual Studio. So I copied the .csproj
files rather than include both imports with conditionals. This also
keeps the LocalDev stuff out of the main/official build scripts.)

LibGit2 DLL/PDB files are copied over from the local peer repo and
named "git2-localdev" rather than using a hash value.

################################################################
#### To work using the LocalDev scripts.
#### [1] Clone both repos as peers and checkout whatever commit you need.

> cd <wherever>
> git clone https://github.com/libgit2/libgit2.git      libgit2
> git clone https://github.com/libgit2/libgit2sharp.git libgit2sharp

#### [2] Build 32-bit and/or 64-bit debug version of LibGit2 into 
####     libgit2/build and libgit2/build64.  You only need to build
####     the bitness that you need, but we support both.
####     TODO add build64 to the libgit2/.gitignores.

> cd libgit2
> mkdir build
> cd build
> cmake -G "Visual Studio 14 2015" -DSTDCALL=ON ..
> msbuild /m libgit2.sln

> cd ..
> mkdir build64
> cd build64
> cmake -G "Visual Studio 14 2015 Win64" -DSTDCALL=ON ..
> msbuild /m libgit2.sln

#### [3] Build LibGit2Sharp and run the unit tests using the above
####     local version of LibGit2.

> build.libgit2sharp.LocalDev.cmd

################################################################
#### Notes.
#### [1] The .cmd will run NuGet on the .sln to fetch all required packages.
####     Per https://docs.nuget.org/consume/command-line-reference
####     (1.1) The .sln explicitly causes .\nuget\packages.config to be loaded.
####     (1.2) And $(ProjectDir)\packages.config is loaded for each project
####           referenced by the .sln.  
####           (1.2.1) LibGit2Sharp\packages.config
####           (1.2.2) LibGit2Sharp.Tests\packages.config
####           
####     TODO 1.2.1 causes the LibGit2Sharp.NativeBinaries package to be
####          downloaded. However, we do not need/use it for developer mode.
####          See if NuGet has a conditional syntax to make this clear.
####
#### [2] The .cmd uses a variant of the CI build script to build LibGit2Sharp
####     and run the unit tests.
####     (2.1) I'm forcing Debug mode on LibGit2Sharp, since that's the whole
####           point of this exercise.
####     (2.2) The build process will copy the locally-built 32/64-bit debug
####           DLL and PDB files from the peer LibGit2 during test runs.
####
####           ./LibGit2Sharp/bin/Debug/LibGit2Sharp.{dll,pdb}
####           ./LibGit2Sharp/bin/Debug/NativeBinaries/{amd64,x86}/git2-localdev.{dll,pdb}
####
####           ./LibGit2Sharp.Tests/bin/Debug/LibGit2Sharp.Tests.{dll,pdb}
####           ./LibGit2Sharp.Tests/bin/Debug/NativeBinaries/{amd64,x86}/git2-localdev.{dll,pdb}
####
#### [3] Here's a reference for running the unit tests as a 64-bit process:
####     https://msdn.microsoft.com/en-us/library/ee782531.aspx
