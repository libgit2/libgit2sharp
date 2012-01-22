using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Core {
	internal class IndexerSafeHandle : SafeHandleBase {
		protected override bool ReleaseHandle() {
			NativeMethods.git_indexer_free(handle);
			return true;
		}
	}
}
