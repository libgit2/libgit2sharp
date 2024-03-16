using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A GitObject
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class GitObject : IEquatable<GitObject>, IBelongToARepository
    {
        internal static IDictionary<Type, ObjectType> TypeToKindMap =
            new Dictionary<Type, ObjectType>
            {
                { typeof(Commit), ObjectType.Commit },
                { typeof(Tree), ObjectType.Tree },
                { typeof(Blob), ObjectType.Blob },
                { typeof(TagAnnotation), ObjectType.Tag },
            };
        internal static IDictionary<Type, GitObjectType> TypeToGitKindMap =
            new Dictionary<Type, GitObjectType>
            {
                { typeof(Commit), GitObjectType.Commit },
                { typeof(Tree), GitObjectType.Tree },
                { typeof(Blob), GitObjectType.Blob },
                { typeof(TagAnnotation), GitObjectType.Tag },
            };

        private static readonly LambdaEqualityHelper<GitObject> equalityHelper =
            new LambdaEqualityHelper<GitObject>(x => x.Id);

        private readonly ILazy<bool> lazyIsMissing;

        /// <summary>
        /// The <see cref="Repository"/> containing the object.
        /// </summary>
        internal readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected GitObject()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitObject"/> class.
        /// </summary>
        /// <param name="repo">The <see cref="Repository"/> containing the object.</param>
        /// <param name="id">The <see cref="ObjectId"/> it should be identified by.</param>
        protected GitObject(Repository repo, ObjectId id)
        {
            this.repo = repo;
            Id = id;
            lazyIsMissing = GitObjectLazyGroup.Singleton(repo, id, handle => handle == null, throwIfMissing: false);
        }

        /// <summary>
        /// Gets the id of this object
        /// </summary>
        public virtual ObjectId Id { get; private set; }

        /// <summary>
        ///  Determine if the object is missing
        /// </summary>
        /// <remarks>
        /// This is common when dealing with partially cloned repositories as blobs or trees could be missing
        /// </remarks>
        public virtual bool IsMissing => lazyIsMissing.Value;

        /// <summary>
        /// Gets the 40 character sha1 of this object.
        /// </summary>
        public virtual string Sha => Id.Sha;

        internal static GitObject BuildFrom(Repository repo, ObjectId id, GitObjectType type, string path)
        {
            switch (type)
            {
                case GitObjectType.Commit:
                    return new Commit(repo, id);

                case GitObjectType.Tree:
                    return new Tree(repo, id, path);

                case GitObjectType.Tag:
                    return new TagAnnotation(repo, id);

                case GitObjectType.Blob:
                    return new Blob(repo, id);

                default:
                    throw new LibGit2SharpException("Unexpected type '{0}' for object '{1}'.",
                                                    type,
                                                    id);
            }
        }

        internal T Peel<T>(bool throwOnError) where T : GitObject
        {
            GitObjectType kind;
            if (!TypeToGitKindMap.TryGetValue(typeof(T), out kind))
            {
                throw new ArgumentException("Invalid type passed to peel");
            }

            using (var handle = Proxy.git_object_peel(repo.Handle, Id, kind, throwOnError))
            {
                if (handle == null)
                {
                    return null;
                }

                return (T)BuildFrom(this.repo, Proxy.git_object_id(handle), kind, null);
            }
        }

        /// <summary>
        /// Peel this object to the specified type
        ///
        /// It will throw if the object cannot be peeled to the type.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="GitObject"/> to peel to.</typeparam>
        /// <returns>The peeled object</returns>
        public virtual T Peel<T>() where T : GitObject
        {
            return Peel<T>(true);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="GitObject"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="GitObject"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="GitObject"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as GitObject);
        }

        /// <summary>
        /// Determines whether the specified <see cref="GitObject"/> is equal to the current <see cref="GitObject"/>.
        /// </summary>
        /// <param name="other">The <see cref="GitObject"/> to compare with the current <see cref="GitObject"/>.</param>
        /// <returns>True if the specified <see cref="GitObject"/> is equal to the current <see cref="GitObject"/>; otherwise, false.</returns>
        public bool Equals(GitObject other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="GitObject"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="GitObject"/> to compare.</param>
        /// <param name="right">Second <see cref="GitObject"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(GitObject left, GitObject right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="GitObject"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="GitObject"/> to compare.</param>
        /// <param name="right">Second <see cref="GitObject"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(GitObject left, GitObject right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns the <see cref="Id"/>, a <see cref="string"/> representation of the current <see cref="GitObject"/>.
        /// </summary>
        /// <returns>The <see cref="Id"/> that represents the current <see cref="GitObject"/>.</returns>
        public override string ToString()
        {
            return Id.ToString();
        }

        private string DebuggerDisplay
        {
            get { return Id.ToString(7); }
        }

        IRepository IBelongToARepository.Repository { get { return repo; } }
    }
}
