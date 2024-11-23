using System;
using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class SetErrorFixture : BaseFixture
    {

        private const string simpleExceptionMessage = "This is a simple exception message.";
        private const string aggregateExceptionMessage = "This is aggregate exception.";
        private const string outerExceptionMessage = "This is an outer exception.";
        private const string innerExceptionMessage = "This is an inner exception.";
        private const string innerExceptionMessage2 = "This is inner exception #2.";

        private const string expectedInnerExceptionHeaderText = "Inner Exception:";
        private const string expectedAggregateExceptionHeaderText = "Contained Exception:";
        private const string expectedAggregateExceptionsHeaderText = "Contained Exceptions:";

        [Fact]
        public void FormatSimpleException()
        {
            Exception exceptionToThrow = new Exception(simpleExceptionMessage);
            string expectedMessage = simpleExceptionMessage;

            AssertExpectedExceptionMessage(expectedMessage, exceptionToThrow);
        }

        [Fact]
        public void FormatExceptionWithInnerException()
        {
            Exception exceptionToThrow = new Exception(outerExceptionMessage, new Exception(innerExceptionMessage));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(outerExceptionMessage);
            sb.AppendLine();
            AppendIndentedLine(sb, expectedInnerExceptionHeaderText, 0);
            AppendIndentedText(sb, innerExceptionMessage, 1);
            string expectedMessage = sb.ToString();

            AssertExpectedExceptionMessage(expectedMessage, exceptionToThrow);
        }

        [Fact]
        public void FormatAggregateException()
        {
            Exception exceptionToThrow = new AggregateException(aggregateExceptionMessage, new Exception(innerExceptionMessage), new Exception(innerExceptionMessage2));

            StringBuilder sb = new StringBuilder();
#if NETFRAMEWORK
            sb.AppendLine(aggregateExceptionMessage);
#else
            sb.AppendLine($"{aggregateExceptionMessage} ({innerExceptionMessage}) ({innerExceptionMessage2})");
#endif
            sb.AppendLine();

            AppendIndentedLine(sb, expectedAggregateExceptionsHeaderText, 0);

            AppendIndentedLine(sb, innerExceptionMessage, 1);
            sb.AppendLine();

            AppendIndentedText(sb, innerExceptionMessage2, 1);

            string expectedMessage = sb.ToString();

            AssertExpectedExceptionMessage(expectedMessage, exceptionToThrow);
        }

        private void AssertExpectedExceptionMessage(string expectedMessage, Exception exceptionToThrow)
        {
            Exception thrownException = null;

            ObjectId id = new ObjectId("deadbeefdeadbeefdeadbeefdeadbeefdeadbeef");

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                repo.ObjectDatabase.AddBackend(new ThrowingOdbBackend(exceptionToThrow), priority: 1);

                try
                {
                    repo.Lookup<Blob>(id);
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                }
            }

            Assert.NotNull(thrownException);
            Assert.Equal(expectedMessage, thrownException.Message);
        }

        private void AppendIndentedText(StringBuilder sb, string text, int indentLevel)
        {
            sb.AppendFormat("{0}{1}", IndentString(indentLevel), text);
        }

        private void AppendIndentedLine(StringBuilder sb, string text, int indentLevel)
        {
            sb.AppendFormat("{0}{1}{2}", IndentString(indentLevel), text, Environment.NewLine);
        }

        private string IndentString(int level)
        {
            return new string(' ', level * 4);
        }

        #region ThrowingOdbBackend

        private class ThrowingOdbBackend : OdbBackend
        {
            private Exception exceptionToThrow;

            public ThrowingOdbBackend(Exception exceptionToThrow)
            {
                this.exceptionToThrow = exceptionToThrow;
            }

            protected override OdbBackendOperations SupportedOperations
            {
                get
                {
                    return OdbBackendOperations.Read |
                        OdbBackendOperations.ReadPrefix |
                        OdbBackendOperations.Write |
                        OdbBackendOperations.WriteStream |
                        OdbBackendOperations.Exists |
                        OdbBackendOperations.ExistsPrefix |
                        OdbBackendOperations.ForEach |
                        OdbBackendOperations.ReadHeader;
                }
            }

            public override int Read(ObjectId oid, out UnmanagedMemoryStream data, out ObjectType objectType)
            {
                throw this.exceptionToThrow;
            }

            public override int ReadPrefix(string shortSha, out ObjectId id, out UnmanagedMemoryStream data, out ObjectType objectType)
            {
                throw this.exceptionToThrow;
            }

            public override int Write(ObjectId oid, Stream dataStream, long length, ObjectType objectType)
            {
                throw this.exceptionToThrow;
            }

            public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
            {
                throw this.exceptionToThrow;
            }

            public override bool Exists(ObjectId oid)
            {
                throw this.exceptionToThrow;
            }

            public override int ExistsPrefix(string shortSha, out ObjectId found)
            {
                throw this.exceptionToThrow;
            }

            public override int ReadHeader(ObjectId oid, out int length, out ObjectType objectType)
            {
                throw this.exceptionToThrow;
            }

            public override int ReadStream(ObjectId oid, out OdbBackendStream stream)
            {
                throw this.exceptionToThrow;
            }

            public override int ForEach(ForEachCallback callback)
            {
                throw this.exceptionToThrow;
            }
        }

        #endregion

    }
}
