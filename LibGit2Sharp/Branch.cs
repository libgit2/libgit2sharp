using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A branch is a special kind of reference
    /// </summary>
    public class Branch
    {
        private readonly Repository repo;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Branch" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal Branch(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the name of the remote (null for local branches).
        /// </summary>
        public string RemoteName { get; private set; }

        /// <summary>
        ///   Gets the name of this branch.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///   Gets the reference for this branch.
        /// </summary>
        public DirectReference Reference { get; private set; }

        /// <summary>
        ///   Gets the commits on this branch. (Starts walking from the References's target).
        /// </summary>
        public CommitCollection Commits
        {
            get { return repo.Commits.StartingAt(this); }
        }

        /// <summary>
        ///   Gets the type of this branch.
        /// </summary>
        public BranchType Type { get; private set; }

        internal static Branch CreateBranchFromReference(Reference reference, Repository repo)
        {
            var tokens = reference.Name.Split('/');
            if (tokens.Length < 2)
            {
                throw new ArgumentException(string.Format("Unexpected ref name: {0}", reference.Name));
            }

            if (tokens[tokens.Length - 2] == "heads")
            {
                return new Branch(repo)
                           {
                               Name = tokens[tokens.Length - 1],
                               Reference = reference.ResolveToDirectReference(),
                               Type = BranchType.Local
                           };
            }
            return new Branch(repo)
                       {
                           Name = string.Join("/", tokens, tokens.Length - 2, 2),
                           RemoteName = tokens[tokens.Length - 2],
                           Reference = reference.ResolveToDirectReference(),
                           Type = BranchType.Remote
                       };
        }

        /// <summary>
        /// Deletes this branch.
        /// </summary>
        public void Delete()
        {
            repo.Refs.Delete(Reference.Name);
        }
    }
}