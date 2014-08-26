#!/bin/bash
set -ev

sudo sh -c "echo 'deb http://download.opensuse.org/repositories/home:/tpokorra:/mono/xUbuntu_12.04/ /' >> /etc/apt/sources.list.d/mono-opt.list"

curl http://download.opensuse.org/repositories/home:/tpokorra:/mono/xUbuntu_12.04/Release.key | sudo apt-key add -

sudo apt-get update
sudo apt-get install mono-opt cmake
