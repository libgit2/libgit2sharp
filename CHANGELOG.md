# LibGit2Sharp Changelog

**LibGit2Sharp brings all the might and speed of libgit2, a native Git implementation, to the managed world of .Net and Mono.**

 - Source code: <https://github.com/libgit2/libgit2sharp>
 - NuGet package: <http://nuget.org/List/Packages/LibGit2Sharp>

## v0.5

### Additions

 - Add Repository.Index.RetrieveStatus() (#49)
 - Add handling of configuration settings of the repository and retrieval of Remotes (#60)

### Changes

 - Can now enumerate from multiple starting points
 - While enumerating commits, automatically dereference objects to a commit object id
 - Defer resolving of Branch.Tip, Tag.Target and Tag.Annotation
 - Replace usage of ApplicationException with LibGit2Exception
 - Update libgit2 binaries to libgit2/libgit2@35e9407

### Fixes

 - Prevent enumeration of commits from throwing when starting from a tag which points at a blob or a tree (#62)
 - Prevent a branch from being removed if it's the current HEAD
 - References are now being enumerated in a ordered way
 - Fix Repository.Discover() implementation when no .git folder exists

## v0.4

### Additions

 - Add Repository.Index.Move()
 - Add handling of abbreviated identifiers
 - Add Repository.Discover() (#25)
 - Add TreeEntry.Type

### Changes

 - Propagate libgit2 error messages upward
 - Update libgit2 binaries to libgit2/libgit2@33afca4

### Fixes

 - Prevents GitSharp from throwing when browsing a repository initialized with LibGit2Sharp (#56)
 - Hide the .git directory when initializing a new standard repository (#53)
 - Fix Repository.IsEmpty implementation when HEAD is in detached state (#52)
 - Relaxed handling of bogus signatures (#51)
 - Improve Mono compatibility (#46 and #47)
 - Remove dependency to msvcr100.dll

## v0.3

### Additions

 - Add basic Commit feature (#32)
 - Add Repository.Index.Unstage()
 - Add branch renaming feature
 - Add symbolsource.org support (#37)

### Changes

 - Make Repository.Head return a Branch instead of a Reference
 - Defer resolving of Repository.Info
 - Update libgit2 binaries to libgit2/libgit2@a5aa5bd
 - Improved Mono compatibility (#34)

### Fixes

 - Add required msvcr100.dll dependency (#43)
 - Fix index updating issue
 - Fix branch creation issue

## v0.2.0

### Changes

 - Update CommitCollection API to query commits
 - Update libgit2 binaries to libgit2/libgit2@4191d52

### Fixes

 - Fix Repository.Info.IsEmpty
 - Fix default CommitCollection sorting behavior
 - Fix creation of reference to prevent it from choking on corrupted ones
 - Fix interop issue in a IIS hosted application

## v0.1.1

### Additions

  - Update staging mechanism to authorize full paths to be used (#23)

### Fixes

 - Fix NuGet packaging

## v0.1.0

 - Initial release
