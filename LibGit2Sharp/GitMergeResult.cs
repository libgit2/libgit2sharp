using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class GitMergeResult
    {
        public GitMergeResult(){}
        internal GitMergeResult(GitMergeResultHandle handle)
        {
            _handle = handle;
        }

        private readonly GitMergeResultHandle _handle;

        internal GitMergeResultHandle Handle
        {
            get { return _handle; }
        }

        public virtual bool IsUpToDate
        {
            get { return Proxy.git_merge_result_is_uptodate(_handle); }
        }

        public virtual bool IsFastForward
        {
            get { return Proxy.git_merge_result_is_fastforward(_handle); }
        }
    }
}
