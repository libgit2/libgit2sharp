using System;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class LazyFixture
    {
        [Fact]
        public void CanReturnTheValue()
        {
            var lazy = new Lazy<int>(() => 2);
            lazy.Value.ShouldEqual(2);
        }

        [Fact]
        public void IsLazilyEvaluated()
        {
            int i = 0;

            var evaluator = new Func<int>(() => ++i);

            var lazy = new Lazy<int>(evaluator);
            lazy.Value.ShouldEqual(1);
        }

        [Fact]
        public void IsEvaluatedOnlyOnce()
        {
            int i = 0;

            var evaluator = new Func<int>(() => ++i);

            var lazy = new Lazy<int>(evaluator);

            lazy.Value.ShouldEqual(1);
            lazy.Value.ShouldEqual(1);
        }
    }
}
