using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LibGit2Sharp
{
    /// <summary>
    /// Criterias used to filter out and order the commits of the repository when querying its history.
    /// </summary>
    public sealed class CommitFilter
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CommitFilter"/>.
        /// </summary>
        public CommitFilter()
        {
            SortBy = CommitSortStrategies.Time;
            IncludeReachableFrom = "HEAD";
            FirstParentOnly = false;
        }

        /// <summary>
        /// The ordering stragtegy to use.
        /// <para>
        ///   By default, the commits are shown in reverse chronological order.
        /// </para>
        /// </summary>
        public CommitSortStrategies SortBy { get; set; }

        /// <summary>
        /// A pointer to a commit object or a list of pointers to consider as starting points.
        /// <para>
        ///   Can be either a <see cref="string"/> containing the sha or reference canonical name to use,
        ///   a <see cref="Branch"/>, a <see cref="Reference"/>, a <see cref="Commit"/>, a <see cref="Tag"/>,
        ///   a <see cref="TagAnnotation"/>, an <see cref="ObjectId"/> or even a mixed collection of all of the above.
        ///   By default, the <see cref="Repository.Head"/> will be used as boundary.
        /// </para>
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use IncludeReachableFrom instead.")]
        public object Since
        {
            get { return IncludeReachableFrom; }
            set { IncludeReachableFrom = value; }
        }

        /// <summary>
        /// A pointer to a commit object or a list of pointers to consider as starting points.
        /// <para>
        ///   Can be either a <see cref="string"/> containing the sha or reference canonical name to use,
        ///   a <see cref="Branch"/>, a <see cref="Reference"/>, a <see cref="Commit"/>, a <see cref="Tag"/>,
        ///   a <see cref="TagAnnotation"/>, an <see cref="ObjectId"/> or even a mixed collection of all of the above.
        ///   By default, the <see cref="Repository.Head"/> will be used as boundary.
        /// </para>
        /// </summary>
        public object IncludeReachableFrom { get; set; }

        internal IList<object> SinceList
        {
            get { return ToList(IncludeReachableFrom); }
        }

        /// <summary>
        /// A pointer to a commit object or a list of pointers which will be excluded (along with ancestors) from the enumeration.
        /// <para>
        ///   Can be either a <see cref="string"/> containing the sha or reference canonical name to use,
        ///   a <see cref="Branch"/>, a <see cref="Reference"/>, a <see cref="Commit"/>, a <see cref="Tag"/>,
        ///   a <see cref="TagAnnotation"/>, an <see cref="ObjectId"/> or even a mixed collection of all of the above.
        /// </para>
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use ExcludeReachableFrom instead.")]
        public object Until
        {
            get { return ExcludeReachableFrom; }
            set { ExcludeReachableFrom = value; }
        }

        /// <summary>
        /// A pointer to a commit object or a list of pointers which will be excluded (along with ancestors) from the enumeration.
        /// <para>
        ///   Can be either a <see cref="string"/> containing the sha or reference canonical name to use,
        ///   a <see cref="Branch"/>, a <see cref="Reference"/>, a <see cref="Commit"/>, a <see cref="Tag"/>,
        ///   a <see cref="TagAnnotation"/>, an <see cref="ObjectId"/> or even a mixed collection of all of the above.
        /// </para>
        /// </summary>
        public object ExcludeReachableFrom { get; set; }

        internal IList<object> UntilList
        {
            get { return ToList(ExcludeReachableFrom); }
        }

        /// <summary>
        /// Whether to limit the walk to each commit's first parent, instead of all of them
        /// </summary>
        public bool FirstParentOnly { get; set; }

        private static IList<object> ToList(object obj)
        {
            var list = new List<object>();

            if (obj == null)
            {
                return list;
            }

            var types = new[]
                        {
                            typeof(string), typeof(ObjectId),
                            typeof(Commit), typeof(TagAnnotation),
                            typeof(Tag), typeof(Branch), typeof(DetachedHead),
                            typeof(Reference), typeof(DirectReference), typeof(SymbolicReference)
                        };

            if (types.Contains(obj.GetType()))
            {
                list.Add(obj);
                return list;
            }

            list.AddRange(((IEnumerable)obj).Cast<object>());
            return list;
        }
    }
}
