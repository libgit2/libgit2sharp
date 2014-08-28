#!/bin/bash
set -e

CURLOC=$(dirname "$0")

"${CURLOC}"/CI/travis.build.libgit2.sh
"${CURLOC}"/CI/travis.build.libgit2sharp.sh

exit $?
