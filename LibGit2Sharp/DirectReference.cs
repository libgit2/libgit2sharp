using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A DirectReference points directly to a <see cref = "GitObject" />
    /// </summary>
    public class DirectReference : Reference
    {
        private readonly Func<GitObject> targetResolver;
        private bool resolved;
        private GitObject target;

        internal DirectReference(Func<GitObject> targetResolver)
        {
            this.targetResolver = targetResolver;
        }

        /// <summary>
        ///   Gets the target of this <see cref = "DirectReference" />
        /// </summary>
        public GitObject Target
        {
            get
            {
                if (resolved)
                {
                    return target;
                }

                target = targetResolver();
                resolved = true;
                return target;
            }
        }

        /// <summary>
        ///   As a <see cref="DirectReference"/> is already peeled, invoking this will return the same <see cref="DirectReference"/>.
        /// </summary>
        /// <returns>This instance.</returns>
        public override DirectReference ResolveToDirectReference()
        {
            return this;
        }
    }
}