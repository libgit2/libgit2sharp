using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A wrapper around the native GitCheckoutOpts structure. This class is responsible
    /// for the managed objects that the native code points to.
    /// </summary>
    internal class GitCheckoutOptsWrapper : IDisposable
    {
        /// <summary>
        /// Create wrapper around <see cref="GitCheckoutOpts"/> from <see cref="CheckoutOptions"/>.
        /// </summary>
        /// <param name="options">Options to create native GitCheckoutOpts structure from.</param>
        /// <param name="paths">Paths to checkout.</param>
        public GitCheckoutOptsWrapper(IConvertableToGitCheckoutOpts options, FilePath[] paths = null)
        {
            Callbacks = options.GenerateCallbacks();

            if (paths != null)
            {
                PathArray = GitStrArrayManaged.BuildFrom(paths);
            }

            Options = new GitCheckoutOpts
            {
                version = 1,
                checkout_strategy = options.CheckoutStrategy,
                progress_cb = Callbacks.CheckoutProgressCallback,
                notify_cb = Callbacks.CheckoutNotifyCallback,
                notify_flags = options.CheckoutNotifyFlags,
                paths = PathArray.Array,
            };
        }

        /// <summary>
        /// Native struct to pass to libgit.
        /// </summary>
        public GitCheckoutOpts Options { get; set; }

        /// <summary>
        /// The managed class mapping native callbacks into the
        /// corresponding managed delegate.
        /// </summary>
        public CheckoutCallbacks Callbacks { get; private set; }

        /// <summary>
        /// Keep the paths around so we can dispose them.
        /// </summary>
        private GitStrArrayManaged PathArray;

        public void Dispose()
        {
            PathArray.Dispose();
        }

        /// <summary>
        /// Method to translate from <see cref="CheckoutFileConflictStrategy"/> to <see cref="CheckoutStrategy"/> flags.
        /// </summary>
        internal static CheckoutStrategy CheckoutStrategyFromFileConflictStrategy(CheckoutFileConflictStrategy fileConflictStrategy)
        {
            CheckoutStrategy flags = default(CheckoutStrategy);

            switch (fileConflictStrategy)
            {
                case CheckoutFileConflictStrategy.Ours:
                    flags = CheckoutStrategy.GIT_CHECKOUT_USE_OURS;
                    break;
                case CheckoutFileConflictStrategy.Theirs:
                    flags = CheckoutStrategy.GIT_CHECKOUT_USE_THEIRS;
                    break;
                case CheckoutFileConflictStrategy.Merge:
                    flags = CheckoutStrategy.GIT_CHECKOUT_CONFLICT_STYLE_MERGE;
                    break;
                case CheckoutFileConflictStrategy.Diff3:
                    flags = CheckoutStrategy.GIT_CHECKOUT_CONFLICT_STYLE_DIFF3;
                    break;
            }

            return flags;
        }
    }
}
