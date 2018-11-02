#!/bin/bash
set -e

EXTRADEFINE="$1"

# Setting LD_LIBRARY_PATH to the current working directory is needed to run
# the tests successfully in linux. Without this, mono can't find libgit when
# the libgit2sharp assembly has been shadow copied. OS X includes the current
# working directory in its library search path, so it works without this value.
export LD_LIBRARY_PATH=.

# Build release for the code generator and the product itself.
export Configuration=release

# On linux we don't pack because we can't build for net40.
# We just build for CoreCLR and run tests for it.
dotnet restore
dotnet build LibGit2Sharp.Tests -f netcoreapp2.0 -property:ExtraDefine="$EXTRADEFINE" -fl -flp:verbosity=detailed
dotnet test LibGit2Sharp.Tests/LibGit2Sharp.Tests.csproj -f netcoreapp2.0 --no-restore --no-build 

exit $?
