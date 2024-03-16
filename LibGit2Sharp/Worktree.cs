using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// A Worktree.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Worktree : IEquatable<Worktree>, IBelongToARepository
    {
        private static readonly LambdaEqualityHelper<Worktree> equalityHelper =
            new LambdaEqualityHelper<Worktree>(x => x.Name);

        private readonly Repository parent;
        //private readonly Repository worktree;
        private readonly string name;
        private WorktreeLock worktreeLock;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Worktree()
        { }

        internal Worktree(Repository repo, string name, WorktreeLock worktreeLock)
        {
            this.parent = repo;
            this.name = name;
            this.worktreeLock = worktreeLock;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal WorktreeHandle GetWorktreeHandle()
        {
            return Proxy.git_worktree_lookup(parent.Handle, name);
        }

        /// <summary>
        /// The name of the worktree.
        /// </summary>
        public virtual string Name { get { return name; } }

        /// <summary>
        /// The Repository representation of the worktree
        /// </summary>
        public virtual Repository WorktreeRepository { get { return new Repository(GetWorktreeHandle()); } }

        /// <summary>
        /// A flag indicating if the worktree is locked or not.
        /// </summary>
        public virtual bool IsLocked { get { return worktreeLock == null ? false : worktreeLock.IsLocked; } }

        /// <summary>
        /// Gets the reason associated with the lock
        /// </summary>
        public virtual string LockReason { get { return worktreeLock == null ? null : worktreeLock.Reason; } }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="Worktree"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="Worktree"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="Worktree"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Worktree);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Worktree"/> is equal to the current <see cref="Worktree"/>.
        /// </summary>
        /// <param name="other">The <see cref="Worktree"/> to compare with the current <see cref="Worktree"/>.</param>
        /// <returns>True if the specified <see cref="Worktree"/> is equal to the current <see cref="Worktree"/>; otherwise, false.</returns>
        public bool Equals(Worktree other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///  Unlock the worktree
        /// </summary>
        public virtual void Unlock()
        {
            using (var handle = GetWorktreeHandle())
            {
                Proxy.git_worktree_unlock(handle);
                this.worktreeLock = Proxy.git_worktree_is_locked(handle);
            }
        }
        
        /// <summary>
        ///  Lock the worktree
        /// </summary>
        public virtual void Lock(string reason)
        {
            using (var handle = GetWorktreeHandle())
            {
                Proxy.git_worktree_lock(handle, reason);
                this.worktreeLock = Proxy.git_worktree_is_locked(handle);
            }
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
        /// Returns the <see cref="Name"/>, a <see cref="string"/> representation of the current <see cref="Worktree"/>.
        /// </summary>
        /// <returns>The <see cref="Name"/> that represents the current <see cref="Worktree"/>.</returns>
        public override string ToString()
        {
            return Name;
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} => {1}", Name, worktreeLock);
            }
        }

        IRepository IBelongToARepository.Repository { get { return parent; } }
    }
}
