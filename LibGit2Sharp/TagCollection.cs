using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        ///   Gets the <see cref = "LibGit2Sharp.Tag" /> with the specified name.
        /// </summary>
        public Tag this[string name]
        {
            get
            {
                EnsureTagName(name);

                var reference = repo.Refs[string.Format("refs/tags/{0}", name)];
                return Tag.CreateTagFromReference(reference, repo);
            }
        }

        #region IEnumerable<Tag> Members

        public IEnumerator<Tag> GetEnumerator()
        {
            var list = repo.Refs.Where(IsATag).Select(p => Tag.CreateTagFromReference(p, repo));
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Creates a tag with the specified name.
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

            var objectToTag = repo.Lookup(target);
            var targetOid = objectToTag.Id.Oid;
            GitOid oid;
            var res = NativeMethods.git_tag_create(out oid, repo.Handle, name, ref targetOid, GitObject.TypeToTypeMap[objectToTag.GetType()], tagger.Handle, message);
            Ensure.Success(res);

            return repo.Lookup<Tag>(new ObjectId(oid));
        }

        private static void EnsureTagName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            if (name.Contains("/")) throw new ArgumentException("Tag name cannot contain the character '/'.");
        }

        private static bool IsATag(Reference reference)
        {
            return reference.Name.StartsWith("refs/tags/");
        }
    }
}