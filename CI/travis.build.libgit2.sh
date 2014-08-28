#!/bin/bash
set -e

CURLOC=$(dirname "$0")

LIBGIT2SHA=$(cat "${CURLOC}"/../LibGit2Sharp/libgit2_hash.txt)
SHORTSHA="${LIBGIT2SHA:0:7}"

echo "SHORTSHA=${SHORTSHA}"

rm -rf "${CURLOC}"/../libgit2/build
mkdir "${CURLOC}"/../libgit2/build
pushd "${CURLOC}"/../libgit2/build

cmake -DCMAKE_BUILD_TYPE:STRING=RelWithDebInfo \
      -DTHREADSAFE:BOOL=ON \
      -DBUILD_CLAR:BOOL=OFF \
      -DUSE_SSH=OFF \
      -DLIBGIT2_FILENAME=git2-"$SHORTSHA" \
      -DCMAKE_OSX_ARCHITECTURES="i386;x86_64" \
      ..
cmake --build .

popd
