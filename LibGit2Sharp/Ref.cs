namespace LibGit2Sharp
{
    public class Ref
    {
        public Ref(string canonicalName, string target)
        {
            CanonicalName = canonicalName;
            Target = target;
        }

        public string CanonicalName { get; private set; }
        public string Target { get; private set; }
    }
}
