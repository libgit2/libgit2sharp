// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using LibGit2Sharp.Core;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ConditionalFactAttribute : FactAttribute
    {
        public ConditionalFactAttribute(params Type[] skipConditions)
        {
            foreach (var skipCondition in skipConditions)
            {
                ExecutionCondition condition = (ExecutionCondition)Activator.CreateInstance(skipCondition);
                if (condition.ShouldSkip)
                {
                    Skip = condition.SkipReason;
                    break;
                }
            }
        }
    }

    public abstract class ExecutionCondition
    {
        public abstract bool ShouldSkip { get; }
        public abstract string SkipReason { get; }
    }

    public class NetFramework : ExecutionCondition
    {
        public override bool ShouldSkip => !Platform.IsRunningOnNetFramework();
        public override string SkipReason => ".NET Framework only test";
    }
}
