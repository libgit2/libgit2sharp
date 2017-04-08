#!/bin/bash
set -e

EXTRADEFINE="$1"

# Setting LD_LIBRARY_PATH to the current working directory is needed to run
# the tests successfully in linux. Without this, mono can't find libgit when
# the libgit2sharp assembly has been shadow copied. OS X includes the current
# working directory in its library search path, so it works without this value.
export LD_LIBRARY_PATH=.

dotnet restore
dotnet build LibGit2Sharp.Tests/LibGit2Sharp.Tests.csproj -f netcoreapp1.0
dotnet test LibGit2Sharp.Tests/LibGit2Sharp.Tests.csproj -f netcoreapp1.0 --no-build

exit $?
