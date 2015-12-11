using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Commands
{
    enum CheckoutMode
    {
        DetachHead,
        CheckoutTree,
    }

    class Checkout
    {
        readonly Repository repo;
        readonly Commit commit;
        readonly Tree tree;
        readonly IEnumerable<string> paths;
        readonly CheckoutOptions options;
        readonly CheckoutMode mode;

        /// <summary>
        /// Checkout a commit
        /// 
        /// This will detach HEAD to the given commit.
        /// </summary>
        /// <param name="repo">The repository in which to act</param>
        /// <param name="commit">The commit to checkout</param>
        /// <param name="options">The options for the checkout operation</param>
        public Checkout(Repository repo, Commit commit, CheckoutOptions options)
        {
            this.repo = repo;
            this.commit = commit;
            this.options = options;
            this.mode = CheckoutMode.DetachHead;
        }

        /// <summary>
        /// Checkout files from a commit
        /// 
        /// This will put the files from the commit into the working directory and the index.
        /// </summary>
        /// <param name="repo">The repository in which to act</param>
        /// <param name="commit">The commit to checkout the files from</param>
        /// <param name="paths">List of paths to checkout. Leave null or empty for all</param>
        /// <param name="options">The options for the checkout operation</param>
        public Checkout(Repository repo, Commit commit, IEnumerable<string> paths, CheckoutOptions options)
        {
            this.repo = repo;
            this.tree = commit.Tree;
            this.paths = paths;
            this.options = options;
            this.mode = CheckoutMode.CheckoutTree;
        }

        /// <summary>
        /// Checkout a tree
        /// 
        /// This will put the files from the tree into the working directory and the index.
        /// </summary>
        /// <param name="repo">The repository in which to act</param>
        /// <param name="tree">The tree to checkout</param>
        /// <param name="paths">List of paths to checkout. Leave null or empty for all</param>
        /// <param name="options">The options for the checkout operation</param>
        public Checkout(Repository repo, Tree tree, IEnumerable<string> paths, CheckoutOptions options)
        {
            this.repo = repo;
            this.tree = tree;
            this.paths = paths;
            this.options = options;
            this.mode = CheckoutMode.CheckoutTree;
        }

        void RunCheckoutTree()
        {
            repo.CheckoutTree(tree, null, options);
        }

        void RunDetachHead()
        {
            repo.Checkout(commit, options);
        }

        public void Run()
        {
            switch (mode)
            {
                case CheckoutMode.DetachHead:
                    RunDetachHead();
                    break;
                case CheckoutMode.CheckoutTree:
                    RunCheckoutTree();
                    break;
                default:
                    throw new NotImplementedException("Unimplemented and undefined checkout mode");
            }
        }
    }
}
