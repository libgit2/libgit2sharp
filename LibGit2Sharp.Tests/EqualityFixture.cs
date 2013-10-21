using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class EqualityFixture : BaseFixture
    {
        [Fact]
        public void EqualityHelperCanTestNullInEquals()
        {
            var one = new ObjectWithEquality();
            var two = new ObjectWithEquality();
            var three = new ObjectWithEquality(ObjectId.Zero);
            var four = new ObjectWithEquality(ObjectId.Zero);

            Assert.True(one.Equals(one));
            Assert.True(two.Equals(two));
            Assert.True(three.Equals(four));
            Assert.True(four.Equals(three));
            Assert.False(one.Equals(three));
            Assert.False(three.Equals(one));
        }

        [Fact]
        public void EqualityHelperCanTestNullInHashCode()
        {
            var one = new ObjectWithEquality();
            var two = new ObjectWithEquality();
            var three = new ObjectWithEquality(ObjectId.Zero);
            var four = new ObjectWithEquality(ObjectId.Zero);

            Assert.Equal(one.GetHashCode(), two.GetHashCode());
            Assert.Equal(three.GetHashCode(), four.GetHashCode());
            Assert.NotEqual(one.GetHashCode(), three.GetHashCode());
        }

        private class ObjectWithEquality : GitObject
        {
            private readonly ObjectId id;

            public ObjectWithEquality(ObjectId id = null)
            {
                this.id = id;
            }

            public override ObjectId Id
            {
                get { return id; }
            }
        }
    }
}
