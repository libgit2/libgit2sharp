using System.Collections.Generic;

namespace LibGit2Sharp
{
    public class RemoteCollection
    {
        private readonly Dictionary<string, Remote> remotes = new Dictionary<string, Remote>();

        private readonly Repository repository;

        public RemoteCollection(Repository repository)
        {
            this.repository = repository;
        }

        public Remote this[string name]
        {
            get
            {
                if (!remotes.ContainsKey(name))
                    remotes.Add(name, new Remote(repository.Config, name));
                return remotes[name];
            }
        }
    }
}