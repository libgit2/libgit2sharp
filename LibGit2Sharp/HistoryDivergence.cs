using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds information about the potential ancestor
    /// and distance from it and two specified <see cref="Commit"/>s.
    /// </summary>
    public class HistoryDivergence
    {
        private readonly Lazy<Commit> commonAncestor;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected HistoryDivergence()
        { }

        internal HistoryDivergence(Repository repo, Commit one, Commit another)
        {
            commonAncestor = new Lazy<Commit>(() => repo.ObjectDatabase.FindMergeBase(one, another));
            Tuple<int?, int?> div = Proxy.git_graph_ahead_behind(repo.Handle, one, another);

            One = one;
            Another = another;
            AheadBy = div.Item1;
            BehindBy = div.Item2;
        }

        /// <summary>
        /// Gets the <see cref="Commit"/> being used as a reference.
        /// </summary>
        public virtual Commit One { get; private set; }

        /// <summary>
        /// Gets the <see cref="Commit"/> being compared against <see cref="HistoryDivergence.One"/>.
        /// </summary>
        public virtual Commit Another { get; private set; }

        /// <summary>
        /// Gets the number of commits that are reachable from <see cref="HistoryDivergence.One"/>,
        /// but not from <see cref="HistoryDivergence.Another"/>.
        /// <para>
        ///   This property will return <c>null</c> when <see cref="HistoryDivergence.One"/>
        ///   and <see cref="HistoryDivergence.Another"/> do not share a common ancestor.
        /// </para>
        /// </summary>
        public virtual int? AheadBy { get; private set; }

        /// <summary>
        /// Gets the number of commits that are reachable from <see cref="HistoryDivergence.Another"/>,
        /// but not from <see cref="HistoryDivergence.One"/>.
        /// <para>
        ///   This property will return <c>null</c> when <see cref="HistoryDivergence.One"/>
        ///   and <see cref="HistoryDivergence.Another"/> do not share a common ancestor.
        /// </para>
        /// </summary>
        public virtual int? BehindBy { get; private set; }

        /// <summary>
        /// Returns the best possible common ancestor <see cref="Commit"/> of <see cref="HistoryDivergence.One"/>
        /// and <see cref="HistoryDivergence.Another"/> or null if none found.
        /// </summary>
        public virtual Commit CommonAncestor
        {
            get { return commonAncestor.Value; }
        }
    }

    internal class NullHistoryDivergence : HistoryDivergence
    {
        public override Commit CommonAncestor
        {
            get { return null; }
        }
    }
}
