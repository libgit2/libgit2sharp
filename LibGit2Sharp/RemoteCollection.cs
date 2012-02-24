using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class RemoteCollection
    {
        private readonly Repository repository;

        internal RemoteCollection(Repository repository)
        {
            this.repository = repository;
        }

        public Remote this[string name]
        {
            get { return RemoteForName(name); }
        }

        internal RemoteSafeHandle LoadRemote(string name, bool throwsIfNotFound)
        {
            RemoteSafeHandle handle;

            int res = NativeMethods.git_remote_load(out handle, repository.Handle, name);

            if (res == (int)GitErrorCode.GIT_ENOTFOUND && !throwsIfNotFound)
            {
                return null;
            }

            Ensure.Success(res);

            return handle;
        }

        private Remote RemoteForName(string name)
        {
            using (RemoteSafeHandle handle = LoadRemote(name, false))
            {
                return Remote.CreateFromPtr(handle);
            }
        }
    }
}
