using LibGit2Sharp.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// An exception thrown that corresponds to a libgit2 (native library) error.
    /// </summary>
    [Serializable]
    public abstract class NativeException : LibGit2SharpException
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected NativeException()
        { }

        internal NativeException(string message)
            : base(message)
        { }

        internal NativeException(string message, Exception innerException)
            : base(message, innerException)
        { }

        internal NativeException(string format, params object[] args)
            : base(format, args)
        {
        }

        internal NativeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        internal NativeException(string message, GitErrorCategory category) : this(message)
        {
            Data.Add("libgit2.category", (int)category);
        }

        internal abstract GitErrorCode ErrorCode { get; }
    }
}
