# LibGit2Sharp Changes

## v0.30 - ([diff](https://github.com/libgit2/libgit2sharp/compare/0.29.0..0.30.0))

### Changes
- This release includes [libgit2 v1.7.2](https://github.com/libgit2/libgit2/releases/tag/v1.7.2).
- Updates for trimming compatibility [#2084](https://github.com/libgit2/libgit2sharp/pull/2084)
- Updates for .NET 8 [#2085](https://github.com/libgit2/libgit2sharp/pull/2085)

## v0.29 - ([diff](https://github.com/libgit2/libgit2sharp/compare/0.28.0..0.29.0))

### Changes
- This release includes [libgit2 v1.7.1](https://github.com/libgit2/libgit2/releases/tag/v1.7.1).
  - CI changes for the native binaries has removed support for CentOS 7. See [#2066](https://github.com/libgit2/libgit2sharp/pull/2066) for details.

### Additions
- Add proxy options [#2065](https://github.com/libgit2/libgit2sharp/pull/2065)
  - See PR for details, including some breaking changes to `CloneOptions` and `SubmoduleUpdateOptions`

## v0.28 - ([diff](https://github.com/libgit2/libgit2sharp/compare/0.27.2..0.28.0))

### Additions
- Add CustomHeaders to PushOptions [#2052](https://github.com/libgit2/libgit2sharp/pull/2052)

## v0.27.2 - ([diff](https://github.com/libgit2/libgit2sharp/compare/0.27.1..0.27.2))

### Changes
- This release includes [libgit2 v1.6.4](https://github.com/libgit2/libgit2/releases/tag/v1.6.4).

### Fixes
- Can't access GIT config (Repository.Config) since v0.27.0 [#2031](https://github.com/libgit2/libgit2sharp/issues/2031)

## v0.27.1 - ([diff](https://github.com/libgit2/libgit2sharp/compare/0.27.0..0.27.1))

### Fixes
- AssemblyVersion of v0.27.0 is `0.0.0.0`, which is lower than the AssemblyVersion of the v0.26.x releases. [#2030](https://github.com/libgit2/libgit2sharp/pull/2030)

## v0.27 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.26..0.27.0))

### Changes
- LibGit2Sharp now targets .NET Framework 4.7.2 and .NET 6.
- This release includes [libgit2 v1.6.3](https://github.com/libgit2/libgit2/releases/tag/v1.6.3).
- Changes to the native binaries let LibGit2Sharp work on all [.NET 6 supported OS versions and architectures](https://github.com/dotnet/core/blob/main/release-notes/6.0/supported-os.md).
- `GlobalSetings.NativeLibraryPath` used to automatically append architecture to the path when running on .NET Framework. This behavior has been removed to make it consistent. [#1918](https://github.com/libgit2/libgit2sharp/pull/1918)

### Additions
- Add support for adding and clearing multi-valued configuration [#1720](https://github.com/libgit2/libgit2sharp/pull/1720)
- added lines and deleted lines in content changes [#1790](https://github.com/libgit2/libgit2sharp/pull/1790)
- Set / get supported extensions [#1908](https://github.com/libgit2/libgit2sharp/pull/1908)
- Simplify dealing with missing git objects [#1909](https://github.com/libgit2/libgit2sharp/pull/1909)
- Throw NotFoundException if trees are missing when computing diff [#1936](https://github.com/libgit2/libgit2sharp/pull/1936)

### Fixes
- Adjust GitStatusOptions to match structure of native libgit2 [#1884](https://github.com/libgit2/libgit2sharp/pull/1884)
- Update git_worktree_add_options struct to include ref pointer [#1890](https://github.com/libgit2/libgit2sharp/pull/1890)
- Fix git_remote_connect not throwing on non-zero result [#1913](https://github.com/libgit2/libgit2sharp/pull/1913)
- Fix incorrect information in exceptions [#1919](https://github.com/libgit2/libgit2sharp/pull/1919)
- Checkout branch looks to remote tracking branches as fallback [#1820](https://github.com/libgit2/libgit2sharp/pull/1820)
- Fixed calling into native libgit2 on osx-arm64 [#1955](https://github.com/libgit2/libgit2sharp/pull/1955)

## v0.26 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.25..v0.26))

### Additions

* Add `CherryPickCommitIntoIndex` to `ObjectDatabase`
* The underlying native library (libgit2) now no longer relies on libcurl
* The underlying native library now no longer relies on zlib
* Add `IndentHeuristic` option to `CompareOptions`

## v0.25 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.24..v0.25))

LibGit2Sharp is now .NET Core 2.0+ and .NET Framework compatible.

### Additions

 - `GitObject` now has a `Peel` method that will let you peel (for example)
    a `Tag` to a `Tree`.
 - `MergeOptions` now includes an option to `IgnoreWhitespaceChanges`.
 - `TreeDefinition` can now `Add` an object with only the ID, which allows
   users of large files to add entries without realizing a `Blob`.
 - `ObjectDatabase` can now `Write` a `Stream`, which allows users of
   large files to stream an object into storage without loading it into
   memory.
 - `ObjectDatabase` can now `MergeCommitsIntoIndex` allowing users to perform
   an in-memory merge that produces an `Index` structure with conflicts.
 - Users can enable or disable dependent object existence checks when
   creating new objects with `GlobalSettings.SetEnableStrictObjectCreation`
 - Users can enable or disable `ofs_delta` support with
   `GlobalSettings.SetEnableOfsDelta`

### Changes

 - Status now does not show untracked files by default.  To retrieve
   untracked files, included the `StatusOptions.IncludeUntracked` and/or
   the `StatusOptions.RecurseUntrackedDirs` options.
 - Status now does not show the ignored files by default.  To retrieve
   ignored files, include the `StatusOptions.IncludeIgnored` option.
 - `Commands.Pull` can now provide a `null` value for `PullOptions`,
   which indicates that default values should be used.

### Fixes

 - The exception thrown when the native library cannot be loaded is now
   able to be caught and will no longer crash the process.
 - Getting the `Notes` collection from a `Repository` no longer throws an
   exception when the repository has no notes.

## v0.24 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.23..v0.24))

This is the last release before a moving to .NET Core compatible library.

It will be the last supported release with the prior architecture; as a
result, this release is primarily bugfixes and does not include major new
APIs.

## v0.23 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.22..v0.23))

### Additions

 - Add `CherryPickCommit` and `RevertCommit` to `ObjectDatabase`.
 - Add `IncludeIgnored` field to `SatusOptions`.
 - Add `Commit.CreateBuffer` to write a commit object to a buffer and
   `ObjectDatabase.CreateCommitWithSignature` to create commits which include a
   signature.
 - Add `Commit.ExtractSignature` to get a commit's signature.
 - Add `ObjectDatabase.Write<T>` to write arbitrary objects to the object db.
 - Add `Commit.PrettifyMessage`


### Changes

 - The native libraries are now expected to be in the `lib` directory,
   instead of `NativeBinaries` for improved mono compatibility.  In
   addition, the names of platform architectures now better reflect
   the vendor naming (eg, `x86_64` instead of `amd64` on Linux).
 - Deprecate the config paths in RepositoryOptions
 - Deprecate the `QueryBy` overload with `FollowFilter`.
 - Deprecate `Branch.Remote` in favour of `Branch.RemoteName`
 - `Remote` no longer implement the equality operator.
 - `Remote.Update` takes a remote name instead of an instance.
 - `Fetch`, `Pull`, `Move`, `Remove`, `Stage` are now in a commands namespace to
   indicate what they represent.

## v0.22 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.21.1...v0.22))

### Additions

 - Add CustomHeaders in the push options (#1217)
 - Expose the minimal diff algorithm (#1229)
 - Expose Reset() with checkout options (#1219)
 - Add a prettify option to history rewrite options (#1185)
 - Add option to describe to only follow the first parent (#1190)
 - Allow setting the config search path (#1123)
 - Provide access to the remote's host HTTPS certificate (#1134)
 - Add support for rebase (#964)
 - ListReferences() now accepts a credentials provider (#1099)
 - Introduce FileStatus.Conflicted and introduce staging of conflicts (#1062)
 - Support streaming filters written in C# (#1030)
 - Add support for the pre-push callback (#1061)
 - Add support for listing remote references without a Repository instance (#1065)
 - Add StashCollection.Apply() and .Pop() (#1068)
 - Support retrieving a configuration for a repository without instantiating it (#1042)
 - Implement 'log --follow'-like functionality (#963)
 - Introduce in-memory merging via Repository.MergeCommits() (#990)
 - Allow setting whether to prune during a fetch (#1258)

### Changes

 - Deprecate MergeConflictException in a backwards-compatible way (#1243)
 - Improve type safety in the generic type for Diff.Compare() (#1180)
 - Obsolete Repository.Commit(), NoteCollection.Add() and
   NoteCollection.Remove() overloads which do not require a signature (#1173)
 - BuildSignature() no longer tries to build a signature from the
   environment if there is none configured (#1171)
 - Rename the commit walker's Since to IncludeReachableFrom and Until to ExcludeReachableFrom (#1069)
 - Rename MergeConflictException to CheckoutConflictException to more
   accurately reflect what it means (#1059)
 - Specify the diff algorithm instead of setting a boolean to use patience (#1043)
 - Remove optional parameters (#1031)
 - Move Repository.Reset(paths) into Index (#959)
 - Move FindMergeBase() overloads to ObjectDatabase (#957)

### Fixes

 - ListReferences() is now able to handle symbolic references (#1132)
 - Repository.IsValid() returns false on empty paths (#1156)
 - The included version of libgit2 includes racy-git support
 - Fix a racy NRE in the filters (#1113)

## v0.21.1 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.21...v0.21.1))

### Changes

- Fix Fetch() related tests to cope with recent GitHub policy change regarding include-tag handling (#995, #1001)

## v0.21 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.20.2...v0.21))

### Additions

 - Introduce repo.Index.Add() and repo.Index.Remove() (#907)
 - Introduce repo.Describe() (#848)
 - Teach Repository.Clone to accept a specific branch to checkout (#650, #882)
 - Expose IndexEntry.AssumeUnchanged (#928, #929)
 - Introduce GlobalSettings.Version.InformationalVersion (#921)

### Changes

 - Deprecate Branch.Checkout() (#937)
 - Deprecate GlobalSettings.Version.MajorMinorPatch (#921)
 - Change Blob.Size output to a long (#892)
 - Update libgit2 binaries to libgit2/libgit2@e0902fb

### Fixes

 - Fix Network.Fetch() tags retrieval (#927)
 - Fix repo.Stage("*") behavior (#907)
 - Plug some memory leaks (#883, #910)
 - Protect Repository.Clone() from null parameters (#891)

## v0.20.2 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.20.1...v0.20.2))

### Fixes

 - Update libgit2 to prevent issues around symbolic links to ".git" folders in trees on Mac

## v0.20.1 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.20...v0.20.1))

### Fixes

 - Update libgit2 to prevent issues around ".git" folders in trees on Windows and Mac

## v0.20 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.19...v0.20))

### Additions

 - Teach RemoteUpdater to update the remote url (#803)
 - Introduce ObjectDatabase.CreateTree(Index) and Index.Reset(Tree) (#788, #799)
 - Add process wide logging feature (#832)
 - Add process wide SmartSubtransport registration/unregistration (#528)
 - Expose Index.Clear() (#814)

### Changes

 - Require Mono 3.6+ on non Windows platform (#800)
 - Require NuGet 2.7+ to install the package (#836)
 - Throw MergeFetchHeadNotFoundException when Pull cannot find ref to merge (#841)
 - Drop Remote.IsSupportedUrl() (#857)
 - Deprecate repo.Version in favor of GlobalSettings.Version (#726, #820)
 - Remove optional parameters from IRepository (#779, #815)
 - Move higher level Index operations to IRepository (#822, #851)
 - Deprecate repo.Refs.Move() in favor of repo.Refs.Rename() (#752, #819)
 - Update libgit2 binaries to libgit2/libgit2@3f8d005

### Fixes

 - Fix compareOptions handling in Diff.Compare<T> (#827, #828)
 - Honor MSBuild Publish mechanism (#597, #821)
 - Make Configuration.BuildSignature() throw a more descriptive message (#831, #858)
 - Prevent Branch.Remote property from throwing when the remote is unresolvable (#823)
 - Teach Revert() to clean up repository state when there is nothing to revert (#816)

## v0.19 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.18.1...v0.19))

### Additions

 - Introduce repo.Network.Remotes.Rename() (#730, #741)
 - Introduce repo.ObjectDatabase.ShortenObjectId() (#677)
 - Introduce Remote.IsSupportedUrl() (#754)
 - Introduce repo.CherryPick() (#755, #756)
 - Expose advanced conflict data (REUC, renames) (#748)

### Changes

 - Make Patch expose a richer PatchEntryChanges type (#686, #702)
 - Make network operations accept Credentials through a callback (#759, #761, #767)
 - Make repo.Index.Stage() respect ignored files by default (#777)
 - Make OdbBackend IDisposable (#713)
 - Update libgit2 binaries to libgit2/libgit2@d28b2b7

### Fixes

 - Don't require specific rights to the parent hierarchy of a repository (#795)
 - Prevent Clone() from choking on empty packets (#794)
 - Ensure Tags can be created in detached Head state (#791)
 - Properly determine object size when calculating its CRC (#783)
 - Prevent blind fast forwards merges when there are checkout conflicts (#781)
 - Make repo.Reset() and repo.Index.Unstage() cope with renamed entries (#777)
 - Do not throw when parsing annotated tags without a Signature (#775, #776)
 - Remove conflicts upon repo.Index.Remove() call (#768)
 - Honor the merge.ff configuration entry (#709)
 - Make Clone() properly throws when passed an invalid url (#701)

## v0.18.1 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.18.0...v0.18.1))

### Additions

 - Make CommitOptions expose additional properties to control how the message should be prettified (#744, #745)

### Changes

 - Update libgit2 binaries to libgit2/libgit2@90befde

### Fixes

 - Fix issue when cloning from a different local volume (#742, #743)

## v0.18.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.17.0...v0.18.0))

### Additions

 - Introduce repo.Revert() (#706)
 - Enhanced control over Merge behavior through MergeOptions (#685)
 - Introduce repo.Network.Remotes.Remove() (#729, #731)
 - Teach repo.Network.ListReferences() to accept a Credentials (#647, #704)
 - Introduce Reference.IsValidName() (#680, #691)
 - Introduce Remote.IsValidName() (#679, #690)
 - Expose StatusOptions.RecurseIgnoredDirs (#728)
 - Introduce GlobalSettings.Features() (#717)
 - Make Repository.Version output the libgit2 built-in features (#676, #694)

### Changes

 - LibGit2Sharp now requires .Net 4.0 (#654, #678)
 - Repository.Checkout() and Branch.Checkout() overloads now accept a CheckoutOptions parameter (#685)
 - Deprecate repo.Refs.IsValidName() (#680, #691)
 - Deprecate repo.Network.Remotes.IsValidName() (#679, #690)
 - Deprecate repo.Branches.Move() in favor of repo.Branches.Rename() (#737, #738)
 - Update libgit2 binaries to libgit2/libgit2@2f6f6eb

### Fixes

 - Do not fail enumerating the ObjectDatabase content when an unexpected file is found under .git/objects (#704)
 - Fix update of HEAD when committing against a bare repository with a temporary working directory (#692)

## v0.17.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.16.0...v0.17.0))

### Additions

 - Introduce Network.Pull() (#643 and #65)
 - Introduce DefaultCredentials for NTLM/Negotiate authentication (#660)
 - Make repo.Merge() accept a Branch (#643)
 - Introduce MergeOptions type, to specify the type of merge and whether to commit or not (#643, #662, #663)
 - Teach reference altering methods to let the caller control how the reflog is valued (#612, #505 and #389)
 - Teach repo.Commits.FindMergeBase to leverage either Standard or Octopus strategy (#634 and #629)
 - Make ObjectDatabase.CreateCommit() accept an option controlling the prettifying of the message (#619)
 - Allow notes retrieval by namespace and ObjectId (#653)

### Changes

 - Deprecate repo.Commits.FindCommonAncestor() in favor of repo.Commits.FindMergeBase() (#634)
 - Deprecate Network.FetchHeads and Repository.MergeHeads (#643)
 - Repository.Commit() overloads now accept a CommitOptions parameter (#668)
 - Repository.Clone() now accepts a CloneOptions parameter
 - Ease testability by making all GetEnumerator() methods fakeable (#646 and #644)
 - Update libgit2 binaries to libgit2/libgit2@bcc6229

### Fixes

 - Make Branch.Add() and Branch.Move() use the correct indentity to feed the reflog (#612 and #616)
 - Fix NullReferenceException occuring in Repository.Clone (#659 and #635)

## v0.16.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.15.0...v0.16.0))

### Additions

 - Introduce Repository.Merge() (#608 and #620)
 - Teach Diff.Compare<>() to return a PatchStats (#610)

### Changes

 - Speed up NuGet post build copy of the native binaries (#613)

### Fixes

 - Fix Remotes.Add(name, url, refspec) to prevent the creation of a default fetch refspec beside the passed in one (#614)
 - Make LibGit2SharpException.Data expose the correct libgit2 error categories (#601)

## v0.15.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.14.1...v0.15.0))

### Additions

 - Introduce ObjectDatabase.Archive()
 - Introduce Repository.Blame()
 - Introduce ObjectDatabase.CalculateHistoryDivergence()
 - Add Configuration.Find(regexp)
 - Add CommitFilter.FirstParentOnly
 - Expose Configuration.BuildSignature()
 - Add TreeDefinition.Add(string, TreeEntry)
 - Make Remote expose its refspecs

### Changes

 - Make Network.Fetch() accepts optional refspec
 - Extend Network.Fetch() and ListReferences() to allow downloading from a url
 - Allow Network.Push() to control packbuilder parallelism
 - Expose Network.Push() progress reporting
 - Extend RemoteUpdater to allow updation of refspecs
 - Teach Index.RetrieveStatus to detect renames in index and workdir
 - Teach NoteCollection to optionally build a Signature from configuration
 - Add RewriteHistoryOptions.OnSucceeding and OnError
 - Introduce Blob FilteringOptions
 - Rename Blob.ContentAsText() as Blob.GetContentText()
 - Rename Blob.ContentStream() as Blob.GetContentStream()
 - Deprecate Blob.Content
 - Teach Diff.Compare<> to detect renames and copies
 - Split Patch and TreeChanges generation
 - Deprecate ResetOptions in favor of ResetMode.
 - Simplify OdbBackend.ReadPrefix() implementation
 - Deprecate ObjectId.StartsWith(byte[], int) in favor of ObjectId.StartsWith(string)
 - Update libgit2 binaries to libgit2/libgit2@96fb6a6

### Fixes

 - Fix building with Mono on OS X (#557)
 - Make RetrieveStatus() reload on-disk index beforehand (#322 and #519)

## v0.14.1 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.14.0...v0.14.1))

### Changes

 - Rename OrphanedHeadException into UnbornBranchException

### Fixes

 - Fix handling of http->https redirects
 - Make probing for libgit2 binaries work from within the NuGet packages folder
 - Accept submodule paths with native directory separators

## v0.14.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.13.0...v0.14.0))

### Additions

 - Introduce Blob.ContentAsText()
 - Teach repo.Refs.RewriteHistory() to prune empty commits
 - Teach repo.Refs.RewriteHistory() to rewrite symbolic references
 - Teach repo.ObjectDatabase to enumerate GitObjects
 - Teach Branches.Add() and Move() to append to the reflog
 - Honor core.logAllRefUpdates configuration setting
 - Add strongly-typed LockedFileException
 - Add TreeDefinition.Remove(IEnumerable<string>)
 - Introduce ObjectId.StartsWith()
 - Introduce repo.Config.GetValueOrDefault()

### Changes

 - Introduce RewriteHistoryOptions type and make repo.Refs.RewriteHistory() leverage it
 - Introduce CheckoutOptions type and make repo.CheckoutPaths() leverage it
 - Obsolete Blob.ContentAsUnicode and Blob.ContentAsUf8
 - Make OdbBackend interface ObjectId based
 - Update libgit2 binaries to libgit2/libgit2@32e4992

### Fixes

 - Ensure repo.Network.Push() overloads pass the Credentials down the call chain
 - Make SymbolicReference.Target cope with chained symbolic references
 - Do not throw when parsing a Remote with no url
 - Prevent files or directories starting with ! from being ignored
 - Teach Index.Stage to stage files in ignored dirs

## v0.13.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.12.0...v0.13.0))

### Additions

 - Teach Repository to Checkout paths
 - Teach Checkout() to cope with revparse extended syntax leading to references
 - Make Stash expose Base, Index and Untracked commits
 - Teach Repository.Init() to set up a separate git directory
 - Teach checkout to report notifications
 - Create a new repo.Checkout() overload which accepts a Commit object
 - Allow ObjectDatabase.CreateBlob() to limit the number of bytes to consume
 - Make ObjectDatabase.CreateBlob() accept a Stream
 - Introduce repo.Refs.RewriteHistory()
 - Introduce repo.Refs.ReachableFrom()
 - Introduce TreeDefinition.From(Commit)
 - Expose TagFetchMode property on Remote type
 - Add CopyNativeDependencies.targets

### Changes

 - Rename CheckoutOptions into CheckoutModifiers
 - Rename DiffOptions into DiffModifiers
 - Rename StashOptions into StashModifiers
 - Rename GitSortOptions into CommitSortStrategies
 - Rename Filter into CommitFilter
 - Rename ObjectDatabase.CreateTag into ObjectDatabase.CreateTagAnnotation
 - Obsolete repo.Clone() overload which returns a Repository
 - Obsolete repo.Init() overload which returns a Repository
 - Obsolete ObjectDatabase.CreateBlob(BinaryReader, string)
 - Update libgit2 binaries to libgit2/libgit2@7940036

### Fixes

 - Fetch should respect the remote's configured tagopt setting unless explicitly specified

## v0.12.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.11.0...v0.12.0))

### Additions

 - Introduce repo.Info.IsShallow
 - Teach repo.Reset to append to the Reflog
 - Introduce repo.ObjectDatabase.CreateTag()
 - Make repo.Diff.Compare() able to define the expected number of context and interhunk lines (#423)

### Changes

 - Obsolete TreeEntryTargetType.Tag
 - Update libgit2 binaries to libgit2/libgit2@9d9fff3

### Fixes

 - Change probing mechanism to rely on specifically named versions of libgit2 binaries (#241)
 - Ensure that two versions of LibGit2Sharp can run side by side (#241)

## v0.11.0 - ([diff](https://github.com/libgit2/libgit2sharp/compare/v0.10.0...v0.11.0))

### Additions

 - Introduce Repository.Refs.Log()
 - Teach Checkout() and Commit() to append to the reflog
 - Teach Refs.Add(), Refs.UpdateTarget() to optionally append to the reflog
 - Add Repository.Submodules namespace
 - Add submodule support to Index.Stage()
 - Add TreeDefinition.Add(Submodule) and TreeDefinition.AddGitLink()
 - Introduce ExplicitPathsOptions type to control handling of unmatched pathspecs
 - Make Index.Remove(), Index.Unstage()/Stage(), Diff.Compare() and Reset() accept ExplicitPathsOptions
 - Add an indexer to the StashCollection
 - Add the UpstreamBranchCanonicalName property to Branch
 - Make Push accept Branch instances
 - Introduce Reference.IsTag, Reference.IsLocalBranch and Reference.IsRemoteTrackingBranch
 - Add Repository.IsValid()
 - Refine build resilience on Linux

### Changes

 - Obsolete Tree.Trees and Tree.Blobs properties
 - Replace GitObjectType with ObjectType and TreeEntryTargetType
 - Rename TreeEntry.Type and TreeEntryDefinition.Type to *.TargetType
 - Move Repository.Conflicts to Index.Conflicts
 - Move Remote.Fetch() in Repository.Network
 - Modify StashCollection.Remove() to accept an integer param rather than a revparse expression
 - Rename BranchUpdater.Upstream to TrackedBranch
 - Rename BranchUpdater.UpstreamMergeBranch to UpstreamBranch
 - Rename BranchUpdater.UpstreamRemote to Remote

### Fixes

 - Make Commit() append to the reflog (#371)
 - Make Index.Remove() able to only remove from index (#270)
 - Teach Index.Remove() to clear the associated conflicts (#325)
 - Make Index.Remove() able to remove folders (#327)
 - Fix repo.Checkout() when working against repo.Head
 - Fix update of the target of repo.Refs.Head
 - Teach Checkout() to cope with revparse syntax
 - Support TreeEntry.Target for GitLink entries

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
