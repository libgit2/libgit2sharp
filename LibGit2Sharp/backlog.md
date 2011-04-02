# Backlog

### LibGit2Sharp

 - Review Reference equality implementation
 - Refactor the error handling (OutputResult -> Exceptions)
 - Launch Code Analysis (Issues related to interop and marshaling will be worked on once we're able to succesffully exchange non ascii encoded data with libgit2)
 - When properly exported use git_strerror() to feed raised exceptions with a meaningful message.

### Tests

 - Add GitObject equality test suite
 - Ensure former API tests are ported and passing

### Documentation

 - Write some sample API usage similar to http://libgit2.github.com/api.html (C# language pack definition available @ http://shjs.sourceforge.net/)
 
### Miscellaneous

 - Run the tests on a Mono platform (would require proper handling of libgit2 its dependencies binaries)
 