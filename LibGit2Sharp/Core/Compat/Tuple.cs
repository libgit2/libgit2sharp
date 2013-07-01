using System.Collections.Generic;

namespace LibGit2Sharp.Core.Compat
{
    /// <summary>
    /// Represents a 2-tuple, or pair.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    public class Tuple<T1, T2>
    {
        private readonly KeyValuePair<T1, T2> kvp;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tuple{T1,T2}"/> class.
        /// </summary>
        /// <param name="item1">The value of the tuple's first component.</param>
        /// <param name="item2">The value of the tuple's second component.</param>
        public Tuple(T1 item1, T2 item2)
        {
            kvp = new KeyValuePair<T1, T2>(item1, item2);
        }

        /// <summary>
        /// Gets the value of the current <see cref="Tuple{T1,T2}"/> object's second component.
        /// </summary>
        public T2 Item2
        {
            get { return kvp.Value; }
        }

        /// <summary>
        /// Gets the value of the current <see cref="Tuple{T1,T2}"/> object's first component.
        /// </summary>
        public T1 Item1
        {
            get { return kvp.Key; }
        }

        /// <summary>
        /// Returns the hash code for the current <see cref="Tuple{T1,T2}"/> object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return kvp.GetHashCode();
        }

        /// <summary>
        /// Returns a value that indicates whether the current <see cref="Tuple{T1,T2}"/> object is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the specified object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Tuple<T1, T2>))
            {
                return false;
            }
            return kvp.Equals(((Tuple<T1, T2>)obj).kvp);
        }


    }
}
