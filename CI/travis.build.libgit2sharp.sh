#!/bin/bash
set -e

CURLOC=$(dirname "$0")

# The Mono interop probing mechanism relies on absolute paths.
# Below, a tad complicated way (but a cross-platform one) of
# retrieving an absolute path to the libgit2 binaries.
_BINPATH=$(cd "${CURLOC}"/../libgit2/build ; pwd)

export LD_LIBRARY_PATH="$_BINPATH":$LD_LIBRARY_PATH
export DYLD_LIBRARY_PATH="$_BINPATH":$DYLD_LIBRARY_PATH

echo "DYLD_LIBRARY_PATH=${DYLD_LIBRARY_PATH}"
echo "LD_LIBRARY_PATH=${LD_LIBRARY_PATH}"

export MONO_OPTIONS=--debug

xbuild "${CURLOC}"/build.msbuild /t:Deploy

exit $?
