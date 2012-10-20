using System;

namespace LibGit2Sharp.Core
{
    internal class LambdaEqualityHelper<T>
    {
        private readonly Func<T, object>[] equalityContributorAccessors;

        public LambdaEqualityHelper(params Func<T, object>[] equalityContributorAccessors)
        {
            this.equalityContributorAccessors = equalityContributorAccessors;
        }

        public bool Equals(T instance, T other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(instance, other))
            {
                return true;
            }

            if (instance.GetType() != other.GetType())
            {
                return false;
            }

            foreach (Func<T, object> accessor in equalityContributorAccessors)
            {
                if (!Equals(accessor(instance), accessor(other)))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(T instance)
        {
            int hashCode = GetType().GetHashCode();

            unchecked
            {
                foreach (Func<T, object> accessor in equalityContributorAccessors)
                {
                    hashCode = (hashCode*397) ^ accessor(instance).GetHashCode();
                }
            }

            return hashCode;
        }
    }
}
