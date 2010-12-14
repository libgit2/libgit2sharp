# Backlog

### Tests

### Core

 - Handle timezone information in Commit and Person
 - Refactor the error handling (OutputResult -> Exceptions)

### Wrapper

 - Crying for refactoring
 - wrapped_git_apply_tag : Try to use a git_person struct instead of passing every tagger related parameter
 
### Documentation

 - Write some sample API usage similar to http://libgit2.github.com/api.html (C# language pack definition available @ http://shjs.sourceforge.net/)
 
### Miscellaneous

 - Test CMake build (cf. https://github.com/libgit2/libgit2/pull/22)
 - Run the tests on a Mono platform (would require proper handling of libgit2 its dependencies binaries)
 