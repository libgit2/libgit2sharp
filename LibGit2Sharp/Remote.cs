namespace LibGit2Sharp
{
    /// <summary>
    ///   A remote repository whose branches are tracked.
    /// </summary>
    public class Remote
    {
        public Remote(string name, string url)
        {
            Name = name;
            Url = url;
        }

        internal Remote()
        {
        }

        /// <summary>
        ///   Gets the alias of this remote repository.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///   Gets the urls to use to communicate with this remote repository.
        /// </summary>
        public string Url { get; internal set; }
    }
}
