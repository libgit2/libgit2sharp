using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Tag
    /// </summary>
    public class Tag : GitObject
    {
        internal Tag(IntPtr obj, ObjectId id = null)
            : base(obj, id)
        {
            Message = NativeMethods.git_tag_message(obj);
            Name = NativeMethods.git_tag_name(obj);
            Tagger = new Signature(NativeMethods.git_tag_tagger(obj));

            var oidPtr = NativeMethods.git_tag_target_oid(obj);
            var oid = (GitOid) Marshal.PtrToStructure(oidPtr, typeof (GitOid));
            TargetId = new ObjectId(oid);
        }

        /// <summary>
        ///   Gets the name of this tag.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///   Gets the message of this tag.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///   Gets the target id that this tag points to.
        /// </summary>
        public ObjectId TargetId { get; private set; }

        /// <summary>
        ///   Gets the tagger.
        /// </summary>
        public Signature Tagger { get; private set; }

        internal static Tag CreateTagFromReference(Reference reference, Repository repo)
        {
            return reference.ResolveToDirectReference().Target as Tag;
        }
    }
}