# Backlog

### LibGit2Sharp

 - Refactor the error handling (OutputResult -> Exceptions)
 - Launch Code Analysis (Issues related to interop and marshaling will be worked on once we're able to succesffully exchange non ascii encoded data with libgit2)
 - When properly exported use git_strerror() to feed raised exceptions with a meaningful message.
 - Add Resharper settings file
 - Add BranchCollection.Delete(string name)
 - Favor the internal static factory method approach (eg. Reference.CreateFromPtr) over the constructor approach (Tag, Signature, ..)
 - Favor overloads over optional parameters
 - Ensure that types that are not supposed to be built by the Consumer do not expose a constructor.
 - Escape as early as possible from a method. Fight against the arrowhead effect (cf. http://elegantcode.com/2009/08/14/observations-on-the-if-statement/)
 - Retrieve the git_repository.path_repository value and exposes it as the Repository.Path property. The libgit2 path is prettyfied and forced converted to an absolute representation

### Tests

 - Add tests ensuring the behavior of indexers when being passed unknown sha and refs
 - Add GitObject equality test suite
 - Add Reference equality test suite
 - Ensure former API tests are ported and passing

### Documentation

 - Write some sample API usage similar to http://libgit2.github.com/api.html (C# language pack definition available @ http://shjs.sourceforge.net/)
 
### Miscellaneous

 - Run the tests on a Mono platform (would require proper handling of libgit2 its dependencies binaries)
 