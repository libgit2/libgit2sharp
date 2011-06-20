# Backlog

### LibGit2Sharp

 - Build a LibGit2Sharp.Sample NuGet package
 - Maybe : Add to Epoch a DateTimeOffset extension method ToRelativeFormat() in order to show dates relative to the current time, e.g. "2 hours ago". (cf. https://github.com/git/git/blob/master/date.c#L89)
 - Turn duplicated strings "refs/xxx" into properties of a generic Constants helper type
 - Refactor the error handling (OutputResult -> Exceptions)
 - Launch Code Analysis (Issues related to interop and marshaling will be worked on once we're able to succesffully exchange non ascii encoded data with libgit2)
 - Remove usage of ApplicationException
 - https://bugzilla.novell.com/show_bug.cgi?id=566247 prevents MonoDevelop users from benefiting from optional parameters while still target at 3.5
 - https://bugzilla.novell.com/show_bug.cgi?id=324680 generates false-positive warnings regarding xml documentation when LibGit2Sharp is built with xbuild
 - The freeing of a newly created signature pointer doesn't "feel" to be done at the right place.
 - Favor overloads over optional parameters (http://msdn.microsoft.com/en-us/library/ms182135.aspx)
 - Ensure that types that are not supposed to be built by the Consumer do not expose a constructor.
 - Escape as early as possible from a method. Fight against the arrowhead effect (cf. http://elegantcode.com/2009/08/14/observations-on-the-if-statement/)

### Wiki

 - How to integrate LibGit2Sharp in an application (console, web, 32/64 bits...)?
 - Keep "LibGit2Sharp Hitchhiker's Guide to Git" up to date (cf. https://github.com/libgit2/libgit2sharp/wiki/LibGit2Sharp-Hitchhiker%27s-Guide-to-Git)
 - Add a complete example (repo init, open repo, stage, commit, branch, ...)

### Tests

 - Enforce test coverage of BranchCollection using canonical names, remotes, non existing branches.
 - Add tests ensuring the behavior of indexers when being passed unknown sha and refs
 - Add GitObject equality test suite
 - Add Reference equality test suite
 - Remove Ignore attribute from ReferenceFixture.CanMoveAReferenceToADeeperReferenceHierarchy() once git_reference_rename() is fixed
 - Remove Ignore attribute from ReferenceFixture.CanMoveAReferenceToAUpperReferenceHierarchy() once git_reference_rename() is fixed
 
### Miscellaneous

 - Run the build on a Unix platform
 
