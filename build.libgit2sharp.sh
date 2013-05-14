#!/bin/sh

PREVIOUS_LD=$LD_LIBRARY_PATH

LIBGIT2SHA=`cat ./LibGit2Sharp/libgit2_hash.txt`
SHORTSHA=${LIBGIT2SHA:3:7} # yup, a utf-8 BOM

rm -rf cmake-build
mkdir cmake-build && cd cmake-build

cmake -DBUILD_SHARED_LIBS:BOOL=ON -DTHREADSAFE:BOOL=ON -DBUILD_CLAR:BOOL=OFF -DCMAKE_INSTALL_PREFIX=./libgit2-bin -DSONAME_APPEND=$SHORTSHA ../libgit2
cmake --build . --target install

LD_LIBRARY_PATH=$PWD/libgit2-bin/lib:$LD_LIBRARY_PATH
export LD_LIBRARY_PATH

cd ..

echo $LD_LIBRARY_PATH
xbuild CI-build.msbuild /t:Deploy

LD_LIBRARY_PATH=$PREVIOUS_LD
export LD_LIBRARY_PATH
