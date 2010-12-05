using System;

namespace libgit2sharp
{
    public class RepositoryDetails
    {
        private static readonly Func<string, string> Check = path => String.IsNullOrEmpty(path) ? "Undetermined" : path;

        public RepositoryDetails(string repositoryDirectory, string index, string databaseDirectory, string workingDirectory, bool isBare)
        {
            RepositoryDirectory = Check(repositoryDirectory);
            Index = Check(index);
            DatabaseDirectory = Check(databaseDirectory);
            WorkingDirectory = Check(workingDirectory);

            IsBare = isBare;
        }

        public bool IsBare { get; private set; }
        public string RepositoryDirectory { get; private set; }
        public string Index { get; private set; }
        public string DatabaseDirectory { get; private set; }
        public string WorkingDirectory { get; private set; }
    }
}