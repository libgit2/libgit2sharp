# LibGit2Sharp Changelog

 - Source code: <https://github.com/libgit2/libgit2sharp>
 - NuGet package: <http://nuget.org/List/Packages/LibGit2Sharp>

## v0.4

### Additions

 - Add handling of abbreviated identifiers
 - Add Repository.Discover() (#25)
 - Add TreeEntry.Type

### Changes

 - Propagate libgit2 error messages upward
 - Update libgit2 binaries to libgit2/libgit2@2a406ab

### Fixes

 - Improved Mono compatibility (#46 and #47)
 - Dependency to msvcr100.dll is no longer required

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

