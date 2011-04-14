using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of <see cref = "Tag" />s in a <see cref = "Repository" />
    /// </summary>
    public class TagCollection : IEnumerable<Tag>
    {
        private readonly Repository repo;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "TagCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        public TagCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "Tag" /> with the specified name.
        /// </summary>
        public Tag this[string name]
        {
            get
            {
                var reference = repo.Refs[FullReferenceNameFrom(name)];
                return Tag.BuildFromReference(reference);
            }
        }

        #region IEnumerable<Tag> Members

        public IEnumerator<Tag> GetEnumerator()
        {
            var list = repo.Refs.Where(IsATag).Select(Tag.BuildFromReference);
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target.</param>
        /// <param name = "tagger">The tagger.</param>
        /// <param name = "message">The message.</param>
        /// <returns></returns>
        public Tag Create(string name, string target, Signature tagger, string message)
        {
            EnsureTagName(name);
            Ensure.ArgumentNotNullOrEmptyString(target, "target");
            Ensure.ArgumentNotNull(tagger, "tagger");
            Ensure.ArgumentNotNullOrEmptyString(message, "message");

            GitObject objectToTag = RetrieveObjectToTag(target);

            var targetOid = objectToTag.Id.Oid;
            GitOid oid;
            var res = NativeMethods.git_tag_create(out oid, repo.Handle, name, ref targetOid, GitObject.TypeToTypeMap[objectToTag.GetType()], tagger.Handle, message);
            Ensure.Success(res);

            return this[name];
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target.</param>
        /// <returns></returns>
        public Tag Create(string name, string target)
        {
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            GitObject objectToTag = RetrieveObjectToTag(target);

            Reference tagRef = repo.Refs.Create(FullReferenceNameFrom(name), objectToTag.Id);   //TODO: To be replaced by native libgit2 tag_create_lightweight() when available.

            return Tag.BuildFromReference(tagRef);
        }

        private GitObject RetrieveObjectToTag(string target)
        {
            var objectToTag = repo.Lookup(target);

            if (objectToTag == null)
            {
                throw new ApplicationException(String.Format("No object identified by '{0}' can be found in the repository.", target));
            }

            return objectToTag;
        }

        private static void EnsureTagName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            if (name.Contains("/")) throw new ArgumentException("Tag name cannot contain the character '/'.");
        }
        
        private static string FullReferenceNameFrom(string name)
        {
            EnsureTagName(name);
            return string.Format("refs/tags/{0}", name);
        }

        private static bool IsATag(Reference reference)
        {
            return reference.Name.StartsWith("refs/tags/");
        }
    }
}