using System;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class ObjectIdFixture
    {
        [TestCase("DDelORu/9Dw38NA3GCOlUJ7tWx0=", "0c37a5391bbff43c37f0d0371823a5509eed5b1d")]
        [TestCase("FqASNFZ4mrze9Ld1ITwjqL109eA=", "16a0123456789abcdef4b775213c23a8bd74f5e0")]
        public void ToString(string encoded, string expected)
        {
            byte[] id = Convert.FromBase64String(encoded);

            string objectId = ObjectId.ToString(id);

            Assert.AreEqual(expected, objectId);
        }

        [TestCase("0c37a5391bbff43c37f0d0371823a5509eed5b1d", "DDelORu/9Dw38NA3GCOlUJ7tWx0=")]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e0", "FqASNFZ4mrze9Ld1ITwjqL109eA=")]
        public void ToByteArray(string objectId, string expected)
        {
            byte[] id = Convert.FromBase64String(expected);

            byte[] rawId = ObjectId.ToByteArray(objectId);

            CollectionAssert.AreEqual(id, rawId);
        }

        [TestCase("0c37a5391bbff43c37f0d0371823a5509eed5b1d", true)]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e0", true)]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e", false)]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e01", false)]
        [TestCase("16=0123456789abcdef4b775213c23a8bd74f5e0", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsValid(string objectId, bool expected)
        {
            bool result = ObjectId.IsValid(objectId);

            Assert.AreEqual(expected, result);
        }
    }
}