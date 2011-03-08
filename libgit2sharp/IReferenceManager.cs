using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface IReferenceManager
    {
        IList<Ref> RetrieveAll();
        Ref Head { get; }
        Ref Lookup(string referenceName, bool shouldEagerlyPeel);
    }
}
