# LibGit2Sharp releases

## v0.3.0

 - [Add] Add msvcr100.dll dependency
 - [Add] Add basic Commit feature
 - [Add] Add Repository.Index.Unstage() functionality
 - [Add] Add branch renaming feature
 - [Fix] Fix index updating issue
 - [Fix] Fix branch creation issue
 - [Upd] Make Repository.Head return a Branch instead of a Reference
 - [Upd] Defer resolving of Repository.Info
 - [Upd] Update libgit2 binaries to a5aa5bd
 - [Upd] Enhance error reporting

 ## v0.2.0

 - [Fix] Fix Repository.Info.IsEmpty
 - [Fix] Fix default CommitCollection sorting behavior
 - [Fix] Fix creation of reference to prevent it from choking on corrupted ones
 - [Fix] Fix interop issue in a IIS hosted application
 - [Upd] Update CommitCollection API to query commits
 - [Upd] Update libgit2 binaries to 4191d52

## v0.1.1

 - [Fix] Fix NuGet packaging
 - [Add] Update staging mechanism to authorize full paths to be used

## v0.1.0

 - Initial release