using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using LibGit2Sharp.Core;

#if LEAKS_CRTDBG
namespace LibGit2Sharp.Tests
{
    public class CrtDbgFixture : BaseFixture
    {
        [Fact]
        public void CanDetectLeak()
        {
            var path = SandboxStandardTestRepoGitDir();
            int count_before = CrtDbg.Dump(CrtDbg.CrtDbgDumpFlags.SET_MARK, "before");
            using (var repo = new Repository(path))
            {
                // While the repo safe-handle is holding an actual C repo pointer which
                // contains an unknown (to us) number of pointers within it.  So we just
                // confirm that there are some new yet-to-be freed items.
                int count_during = CrtDbg.Dump(
                    CrtDbg.CrtDbgDumpFlags.LEAKS_SINCE_MARK | CrtDbg.CrtDbgDumpFlags.QUIET,
                    "during");
                Assert.True(count_during > count_before);
            }
            // When the repo is released, our memory count should return to what it was.
            // Note we may have to force a GC.
            int count_after = CrtDbg.Dump(
                CrtDbg.CrtDbgDumpFlags.LEAKS_SINCE_MARK | CrtDbg.CrtDbgDumpFlags.QUIET,
                "after");
            Assert.Equal(count_after, count_before);
        }
    }
}
#endif
