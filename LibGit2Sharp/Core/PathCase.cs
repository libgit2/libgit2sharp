using System;

namespace LibGit2Sharp.Core
{
    internal class PathCase
    {
        private readonly StringComparer comparer;
        private readonly StringComparison comparison;

        public PathCase(IRepository repo)
        {
            var value = repo.Config.Get<bool>("core.ignorecase");
            switch (value != null && value.Value)
            {
                case true:
                    comparer = StringComparer.OrdinalIgnoreCase;
                    comparison = StringComparison.OrdinalIgnoreCase;
                    break;

                default:
                    comparer = StringComparer.Ordinal;
                    comparison = StringComparison.Ordinal;
                    break;
            }
        }

        public StringComparer Comparer
        {
            get { return comparer; }
        }

        public bool StartsWith(string path, string value)
        {
            return path != null && path.StartsWith(value, comparison);
        }
    }
}
