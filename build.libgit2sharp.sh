#!/bin/bash

PREVIOUS_LD=$LD_LIBRARY_PATH

LIBGIT2SHA=`cat ./LibGit2Sharp/libgit2_hash.txt`
SHORTSHA=${LIBGIT2SHA:0:7}

rm -rf cmake-build
mkdir cmake-build && cd cmake-build

cmake -DCMAKE_BUILD_TYPE:STRING=RelWithDebInfo -DTHREADSAFE:BOOL=ON -DBUILD_CLAR:BOOL=OFF -DCMAKE_INSTALL_PREFIX=./libgit2-bin -DLIBGIT2_FILENAME=git2-$SHORTSHA ../libgit2
cmake --build . --target install

LD_LIBRARY_PATH=$PWD/libgit2-bin/lib:$LD_LIBRARY_PATH
export LD_LIBRARY_PATH

cd ..

echo $LD_LIBRARY_PATH
xbuild CI-build.msbuild /t:Deploy

EXIT_CODE=$?

LD_LIBRARY_PATH=$PREVIOUS_LD
export LD_LIBRARY_PATH

exit $EXIT_CODE
