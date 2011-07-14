using System;
using LibGit2Sharp.Core;
using NUnit.Framework;
using LibGit2Sharp.Tests.TestHelpers;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class LazyFixture
    {
        [Test]
        public void CanReturnTheValue()
        {
            var lazy = new Lazy<int>(() =>  2);
            lazy.Value.ShouldEqual(2);
        }

        [Test]
        public void IsLazilyEvaluated()
        {
            int i = 0;

            var evaluator = new Func<int>(() => ++i);

            var lazy = new Lazy<int>(evaluator);
            lazy.Value.ShouldEqual(1);
        }

        [Test]
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