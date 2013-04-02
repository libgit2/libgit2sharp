# LibGit2Sharp Changes

**LibGit2Sharp brings all the might and speed of libgit2, a native Git implementation, to the managed world of .Net and Mono.**

 - Source code: <https://github.com/libgit2/libgit2sharp>
 - NuGet package: <http://nuget.org/List/Packages/LibGit2Sharp>
 - Issue tracker: <https://github.com/libgit2/libgit2sharp/issues>
 - CI server: <http://teamcity.codebetter.com/project.html?projectId=project127&guest=1>
 - @libgit2sharp: <http://twitter.com/libgit2sharp>

## v0.10.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.9.5...v0.10.0))

### Additions

 - Update working directory on checkout
 - New network related features: clone, fetch, push, list remote references
 - Expose the heads that have been updated during the last fetch in Repository.Network.FetchHeads
 - Introduce Repository.Network.Remotes.IsValidName()
 - New .gitignore related features: temporary rules, path checking
 - Add support for custom, managed ODB backends
 - Add revparse support in Repository.Lookup()
 - Improve Repository.Commit(): add merged branches as parents, cleanup merge data
 - Introduce Blob.IsBinary
 - Add strongly-typed exceptions (NonFastForwardException, UnmergedIndexEntriesException, ...)
 - Add basic stashing support: add, retrieve, list and remove
 - Add git clean support in Repository.RemoveUntrackedFiles()
 - Add shortcut to HEAD in Repository.Refs.Head
 - Introduce Repository.Refs.IsValidName()
 - Add Repository.Refs.FromGlob() to enumerate references matching a specified glob
 - Add support for XDG configuration store
 - Make Config.Get() and Config.Delete() able to target a specific store
 - Diff.Compare() enhancements: work against workdir and index, consider untracked changes, expose typechanges
 - Allow retrieval of the remote of a non-local branch through Branch.Remote
 - Allow modification of the branch properties through Repository.Branches.Update()
 - Expose merge related information: Repository.Index.IsFullyMerged, Repository.Conflicts, IndexEntry.StageLevel
 - Expose the heads being merged in Repository.MergeHeads
 - Introduce IndexEntry.Mode
 - Add more repository information: Repository.Info.CurrentOperation, Repository.Info.Message, Repository.Info.IsHeadOrphaned
 - Allow passing an optional RepositoryOptions to Repository.Init()
 - Allow reset filtering by passing a list of paths to consider

### Changes

 - Make TreeChanges and TreeEntryChanges expose native paths
 - Make Repository.Reset accept a Commit instead of a string
 - Stop sorting collections (references, remotes, notes ...)
 - Move AheadBy/BehindBy into new Branch.TrackingDetails
 - Move Repository.Remotes to Repository.Network.Remotes
 - Move Configuration.HasXXXConfig() to Configuration.HasConfig()
 - Rename CommitCollection to CommitLog
 - Rename LibGit2Exception to LibGit2SharpException
 - Rename Delete() to Unset() in Configuration
 - Rename Delete() to Remove() in TagCollection, ReferenceCollection, NoteCollection, BranchCollection
 - Rename Create() to Add() in TagCollection, BranchCollection, ReferenceCollection, RemoteCollection, NoteCollection
 - Obsolete RepositoryInformation.IsEmpty, DiffTarget, IndexEntry.State, Commit.ParentsCount

### Fixes

 - Allow abstracting LibGit2Sharp in testing context (#138)
 - Ease the detection of a specific key in a specific store (#162)
 - Expose libgit2 error information through the LibGit2SharpException.Data property(#137)
 - Preserve non-ASCII characters in commit messages (#191)
 - Fix retrieval of the author of a commit (#242)
 - Prevent duplicated tree entries in commits (#243)
 - Fix Repository.Discover behaviour with UNC paths (#256)
 - Make Index.Unstage work against an orphaned head (#257)
 - Make IsTracking & TrackedBranch property not throw for a detached head (#266, #268)

## v0.9.5 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.9.0...v0.9.5))

### Additions

 - Add support to create, retrieve, list and remove object notes (#140)
 - Make Repository able to rely on specified global and system config files (#157)

### Changes

 - Remove repo.Branches.Checkout()
 - Remove Tree.Files
 - Update libgit2 binaries to libgit2/libgit2@4c977a6

### Fixes

 - Allow initialization of a repository located on a network path (#153)

## v0.9 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.8.0...v0.9.0))

### Additions

 - Support local tracking branches (#113)
 - Add an Ignored collection to the RepositoryStatus type (#120)
 - Expose the relative path of TreeEntries (#122)
 - Make Repository able to work against specified index and workdir (#132)
 - Direct creation or Blobs, Trees and Commits without the workdir nor index involvement (#135)
 - New Diff namespace: supports tree-to-tree, tree-to-index and blob-to-blob comparisons (#136)
 - Add Commits.FindCommonAncestor() (#149)

### Changes

 - Deprecate repo.Branches.Checkout() in favor of repo.Checkout()
 - Deprecate Tree.Files in favor of Tree.Blobs
 - Update libgit2 binaries to libgit2/libgit2@7a361e9

### Fixes

 - Embed both x86 and amd64 compiled versions of libgit2 binaries (#55, #70)
 - Honor symbolically linked global .gitconfig (#84)
 - Ease the creation of a remote (#114)
 - Prevent memory issues when revwalking a large repository (#115)
 - Cleanup commit and tag messages (#117)
 - Make RetrieveStatus() return correct results (#123)
 - Allow staging on a network shared repository (#125)

## v0.8 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.7.0...v0.8.0))

### Additions

 - Add Repository.Reset() and support of Soft and Mixed modes
 - Make Repository.Commit() able to amend the current tip of the Head
 - Make the constructor of Repository able to open a repository from a working directory path
 - Make Repository.Index.RetriveStatus honor the .gitgnore files

### Changes

 - Remove Repository.HasObject()
 - Change Repository.Init() to make it return an instance of the Repository type, instead of a string containing the path of the repository
 - Update libgit2 binaries to libgit2/libgit2@6d39c0d

### Fixes

 - Reinit a repository doesn't throw anymore (#54)
 - Embedded libgit2 binaries are now compiled with THREADSAFE=ON flag (#64)
 - Prevent Repository.Head.IsCurrentRepositoryHead from throwing when the Repository is empty (#105)

## v0.7 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.6.2...v0.7.0))

### Additions

 - Allow access to System and Global configuration outside the context of a repo
 - Add overloads to index methods that accept collection of paths

### Changes

 - Make Index.RetrieveStatus() return native file paths
 - Make IndexEntry able to cope with native file paths
 - Update libgit2 binaries to libgit2/libgit2@be00b00
 - Deprecate Repository.HasObject()

### Fixes

 - Fix the build script to be fully XBuild compatible on Linux/Mono 2.10
 - Fix Index.Remove() to correctly handle files which have been deleted and modified in the working directory

## v0.6.2 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.6.1...v0.6.2))

### Fixes

 - Make Index methods (Stage, Unstage, Move... ) able to cope with native Windows directory separator char

## v0.6.1 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.6.0...v0.6.1))

### Changes

 - Update libgit2 binaries to libgit2/libgit2@e3baa3c

### Fixes

 - Prevent segfault when determining the status a of repository
 - Fix retrieval of buggy status in some (not that rare) cases

## v0.6 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.5.0...v0.6.0))

### Additions

 - Add Configuration.Get() overload that takes key in parts
 - Add tracking branch details (#75)
 - Allow creation of commit using signature from configuration files
 - Add Index.Remove() (#78)
 - Add a string indexer to the Commit and Tree types in order to ease retrieval of TreeEntries

### Changes

 - Provide default value for non existent configuration setting (#67)
 - Change the tree structure into which libgit2 binaries are located (#70)
 - Update libgit2 binaries to libgit2/libgit2@28c1451

### Fixes

 - Prevent enumeration of branches from throwing when the repository contains remote branches (#69)
 - Fix Index.Stage(), Index.Unstage() (#78)

## v0.5 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.4.0...v0.5.0))

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

## v0.4 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.3.0...v0.4.0))

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

## v0.3 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.2.0...v0.3.0))

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

## v0.2.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.1.1...v0.2.0))

### Changes

 - Update CommitCollection API to query commits
 - Update libgit2 binaries to libgit2/libgit2@4191d52

### Fixes

 - Fix Repository.Info.IsEmpty
 - Fix default CommitCollection sorting behavior
 - Fix creation of reference to prevent it from choking on corrupted ones
 - Fix interop issue in a IIS hosted application

## v0.1.1 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.1.0...v0.1.1))

### Additions

  - Update staging mechanism to authorize full paths to be used (#23)

### Fixes

 - Fix NuGet packaging

## v0.1.0

 - Initial release
