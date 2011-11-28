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
        private LibGit2Sharp.Core.Lazy<GitObject> targetBuilder;

        internal TagAnnotation(ObjectId id)
            : base(id)
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
        ///   Gets the <see cref = "GitObject" /> that this tag annotation points to.
        /// </summary>
        public GitObject Target
        {
            get { return targetBuilder.Value; }
        }

        /// <summary>
        ///   Gets the tagger.
        /// </summary>
        public Signature Tagger { get; private set; }

        internal static TagAnnotation BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            IntPtr oidPtr = NativeMethods.git_tag_target_oid(obj);
            var oid = (GitOid)Marshal.PtrToStructure(oidPtr, typeof(GitOid));

            return new TagAnnotation(id)
                       {
                           Message = NativeMethods.git_tag_message(obj).MarshallAsString(),
                           Name = NativeMethods.git_tag_name(obj).MarshallAsString(),
                           Tagger = new Signature(NativeMethods.git_tag_tagger(obj)),
                           targetBuilder = new LibGit2Sharp.Core.Lazy<GitObject>(() => repo.Lookup<GitObject>(new ObjectId(oid)))
                       };
        }
    }
}
