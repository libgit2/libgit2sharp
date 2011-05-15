# Backlog

### LibGit2Sharp

 - Add branch renaming (public Branch Move(string oldName, string newName, bool allowOverwrite = false))
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

### Tests

 - Add tests ensuring the behavior of indexers when being passed unknown sha and refs
 - Add GitObject equality test suite
 - Add Reference equality test suite
 - Ensure former API tests are ported and passing
 - Remove Ignore attribute from ReferenceFixture.CanMoveAReferenceToADeeperReferenceHierarchy() once git_reference_rename() is fixed
 - Remove Ignore attribute from ReferenceFixture.CanMoveAReferenceToAUpperReferenceHierarchy() once git_reference_rename() is fixed

### Documentation

 - Write some sample API usage similar to http://libgit2.github.com/api.html (C# language pack definition available @ http://shjs.sourceforge.net/)
 
### Miscellaneous

 - Run the tests on a Mono platform
 
