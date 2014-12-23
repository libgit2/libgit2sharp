#!/bin/bash

LIBGIT2SHA=`cat ./LibGit2Sharp/libgit2_hash.txt`
SHORTSHA=${LIBGIT2SHA:0:7}
EXTRADEFINE="$1"

rm -rf libgit2/build
mkdir libgit2/build
pushd libgit2/build
export _BINPATH=`pwd`

cmake -DCMAKE_BUILD_TYPE:STRING=RelWithDebInfo \
      -DBUILD_CLAR:BOOL=OFF \
      -DUSE_SSH=OFF \
      -DENABLE_TRACE=ON \
      -DLIBGIT2_FILENAME=git2-$SHORTSHA \
      -DCMAKE_OSX_ARCHITECTURES="i386;x86_64" \
      ..
cmake --build .

export LD_LIBRARY_PATH=$_BINPATH:$LD_LIBRARY_PATH
export DYLD_LIBRARY_PATH=$_BINPATH:$DYLD_LIBRARY_PATH

popd

export MONO_OPTIONS=--debug

# Required for NuGet package restore to run.
mozroots --import --sync

echo $DYLD_LIBRARY_PATH
echo $LD_LIBRARY_PATH
xbuild CI/build.msbuild /target:Deploy /property:ExtraDefine="$EXTRADEFINE"

exit $?
