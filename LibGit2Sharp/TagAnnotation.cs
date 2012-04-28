using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A TagAnnotation
    /// </summary>
    public class TagAnnotation : GitObject
    {
        private Lazy<GitObject> targetBuilder;

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

        internal static TagAnnotation BuildFromPtr(GitObjectSafeHandle obj, ObjectId id, Repository repo)
        {
            ObjectId targetOid = NativeMethods.git_tag_target_oid(obj).MarshalAsObjectId();

            return new TagAnnotation(id)
                       {
                           Message = NativeMethods.git_tag_message(obj),
                           Name = NativeMethods.git_tag_name(obj),
                           Tagger = new Signature(NativeMethods.git_tag_tagger(obj)),
                           targetBuilder = new Lazy<GitObject>(() => repo.Lookup<GitObject>(targetOid))
                       };
        }
    }
}
