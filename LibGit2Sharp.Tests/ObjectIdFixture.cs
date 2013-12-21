﻿using System;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ObjectIdFixture
    {
        private const string validSha1 = "ce08fe4884650f067bd5703b6a59a8b3b3c99a09";
        private const string validSha2 = "de08fe4884650f067bd5703b6a59a8b3b3c99a09";

        private static readonly byte[] bytes = new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9 };

        [Theory]
        [InlineData("Dummy", typeof(ArgumentException))]
        [InlineData("", typeof(ArgumentException))]
        [InlineData("8e", typeof(ArgumentException))]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData(validSha1 + "dd", typeof(ArgumentException))]
        public void PreventsFromBuildingWithAnInvalidSha(string malformedSha, Type expectedExceptionType)
        {
            Assert.Throws(expectedExceptionType, () => new ObjectId(malformedSha));
        }

        [Fact]
        public void CanConvertOidToSha()
        {
            var id = new ObjectId(bytes);

            Assert.Equal(validSha1, id.Sha);
            Assert.Equal(validSha1, id.ToString());
        }

        [Fact]
        public void CanConvertShaToOid()
        {
            var id = new ObjectId(validSha1);

            Assert.Equal(bytes, id.RawId);
        }

        [Fact]
        public void CanCastShaToObjectId()
        {
            var id = (ObjectId)validSha1;

            Assert.Equal(bytes, id.RawId);
        }

        [Fact]
        public void CanCastNullToObjectId()
        {
            string sha = null;

            var id = (ObjectId)sha;

            Assert.Null(id);
        }

        [Fact]
        public void CreatingObjectIdWithWrongNumberOfBytesThrows()
        {
            var invalidBytes = new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154 };

            Assert.Throws<ArgumentException>(() => { new ObjectId(invalidBytes); });
        }

        [Fact]
        public void DifferentObjectIdsAreEqual()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha2);

            Assert.False((a.Equals(b)));
            Assert.False((b.Equals(a)));

            Assert.False((a == b));
            Assert.True((a != b));
        }

        [Fact]
        public void DifferentObjectIdsDoesNotHaveSameHashCode()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha2);

            Assert.NotEqual(b.GetHashCode(), a.GetHashCode());
        }

        [Fact]
        public void SimilarObjectIdsAreEqual()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha1);

            Assert.True((a.Equals(b)));
            Assert.True((b.Equals(a)));

            Assert.True((a == b));
            Assert.False((a != b));
        }

        [Fact]
        public void SimilarObjectIdsHaveSameHashCode()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha1);

            Assert.Equal(b.GetHashCode(), a.GetHashCode());
        }

        [Theory]
        [InlineData("Dummy", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("0", false)]
        [InlineData("01", false)]
        [InlineData("012", false)]
        [InlineData("0123", false)]
        [InlineData("0123456", false)]
        [InlineData(validSha1 + "d", false)]
        [InlineData(validSha1, true)]
        public void TryParse(string maybeSha, bool isValidSha)
        {
            ObjectId parsedObjectId;
            bool result = ObjectId.TryParse(maybeSha, out parsedObjectId);
            Assert.Equal(isValidSha, result);

            if (!result)
            {
                return;
            }

            Assert.NotNull(parsedObjectId);
            Assert.Equal(maybeSha, parsedObjectId.Sha);
            Assert.True(maybeSha.StartsWith(parsedObjectId.ToString(3)));
            Assert.Equal(maybeSha, parsedObjectId.ToString(42));
        }

        [Theory]
        [InlineData("d", true)]
        [InlineData("dead", true)]
        [InlineData("deadbe", true)]
        [InlineData("DeAdBEE", true)]
        [InlineData("deff", false)]
        [InlineData("dfff", false)]
        [InlineData("9876", false)]
        [InlineData("This is not a valid short hexified sha!!!!!", false)]
        public void StartsWith(string shortSha, bool expected)
        {
            var id = new ObjectId("deadbeef84650f067bd5703b6a59a8b3b3c99a09");

            Assert.Equal(expected, id.StartsWith(shortSha));
        }
    }
}
