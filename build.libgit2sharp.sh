#!/bin/bash
set -e

EXTRADEFINE="$1"

# Setting LD_LIBRARY_PATH to the current working directory is needed to run
# the tests successfully in linux. Without this, mono can't find libgit when
# the libgit2sharp assembly has been shadow copied. OS X includes the current
# working directory in its library search path, so it works without this value.
export LD_LIBRARY_PATH=.

# Required for NuGet package restore to run.
mozroots --import --sync

mono Lib/NuGet/NuGet.exe restore LibGit2Sharp.sln
xbuild CI/build.msbuild /target:Deploy /property:ExtraDefine="$EXTRADEFINE"

exit $?
