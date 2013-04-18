#!/bin/sh

PREVIOUS_LD=$LD_LIBRARY_PATH

rm -rf cmake-build
mkdir cmake-build && cd cmake-build

cmake -DBUILD_SHARED_LIBS:BOOL=ON -DTHREADSAFE:BOOL=ON -DBUILD_CLAR:BOOL=OFF -DCMAKE_INSTALL_PREFIX=./libgit2-bin ../libgit2
cmake --build . --target install

LD_LIBRARY_PATH=$PWD/libgit2-bin/lib:$LD_LIBRARY_PATH
export LD_LIBRARY_PATH

cd ..

echo $LD_LIBRARY_PATH
xbuild CI-build.msbuild /t:Deploy

LD_LIBRARY_PATH=$PREVIOUS_LD
export LD_LIBRARY_PATH
