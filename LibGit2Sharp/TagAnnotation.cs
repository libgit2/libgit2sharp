using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A TagAnnotation
    /// </summary>
    public class TagAnnotation : GitObject
    {
        internal TagAnnotation(ObjectId id, IntPtr obj)
            : base(obj, id)
        {
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

        internal static TagAnnotation BuildFromPtr(IntPtr obj, ObjectId id)
        {
            var oidPtr = NativeMethods.git_tag_target_oid(obj);
            var oid = (GitOid)Marshal.PtrToStructure(oidPtr, typeof(GitOid));

            return new TagAnnotation(id, obj)
                       {
                           Message = NativeMethods.git_tag_message(obj),
                           Name = NativeMethods.git_tag_name(obj),
                           Tagger = new Signature(NativeMethods.git_tag_tagger(obj)),
                           TargetId = new ObjectId(oid)
                       };
        }
    }
}