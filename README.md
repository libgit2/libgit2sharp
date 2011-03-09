libgit2sharp
============

Description
===========

libgit2sharp provides .NET bindings for libgit2.
[libgit2](http://libgit2.github.com/) itself is a new library
implementing the git data storage model in C.

Goal
====
The goal of the project is to provide up to date .NET bindings
for libgit2. LibGit2Sharp.Core is practically a wrapper around
the basic libgit2 api, while LibGit2Sharp provides easier and safer
usage.

.NET Runtime
============

A .NET 3.5 compatible runtime is required.

Mono 2.6.7 (the version used by Ubuntu and debian) works perfectly.
But the next major versions, 2.8, 2.10, don't work correctly.
It is probably a problem of the mono implementation, since it works
perfectly on windows.

Compilation
===========

All binaries and intermediate products are available in the
repository. Lookup deps and libs. The compiled libgit2 as a windows
binary can be found in deps. MSVCR100.dll is needed as dependency.

The xml output of gccxml and the NativeMethods.cs are all
checked in into the repository, so you don't have to read further on
if you are not compiling against a newer version of libgit2.

In order to compile LibGit2Sharp.Core gccxml is needed. gccxml is a tool
that generates xml output from cpp code. This output is used in order
to autogenerate all the native dll import bindings, since the API
of libgit2 is constantly changing, the assumption was made that this
would be the most effective way to stay up to date with the API. The
project LibGit2.Core.Generator will search for GIT_EXTERN exposed
functions and then use the generated xml to utilize the metadata.
The newest version of gccxml is needed, since earlier versinos
don't have an important buildin type.

In order to generate the xml output, just type the following command
in the resources folder (Makefile is needed):

    make patch
    make

make patch will patch the current source, in order to make it easier to
parse GIT_EXTERN exposed functions.

Now LibGit2.Core.Generator has to be build and ran once. It will generate
NativeMethods.cs for LibGit2.Core. You can do it via the command line:

    LibGit2.Core.Generator.exe ../Git2/NativeMethods

Or just use "Run this item" from your IDE.

Authors
=======

* [nulltoken](https://github.com/nulltoken)
* [Andrius Bentkus](mailto: andrius.bentkus@gmail.com)

Contacts
========

Use the issue tracker on on www.github.com/libgit2/libgit2sharp.
Furthermore, you can reach Andrius Bentkus direvtly via email or IRC,
mostly nick named txdv or bentkus on every major irc network.

License
=======
MIT License. Read LICENSE file.

