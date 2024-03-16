using System;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// An exception thrown that corresponds to a libgit2 (native library) error.
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
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

#if NETFRAMEWORK
        internal NativeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif

        internal NativeException(string message, GitErrorCategory category) : this(message)
        {
            Data.Add("libgit2.category", (int)category);
        }

        internal abstract GitErrorCode ErrorCode { get; }
    }
}
