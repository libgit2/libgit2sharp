﻿using System;
using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
    internal class GitObjectTypeMap : Dictionary<Type, GitObjectType>
    {
        public new GitObjectType this[Type type]
        {
            get { return !ContainsKey(type) ? GitObjectType.Any : base[type]; }
        }
    }
}
