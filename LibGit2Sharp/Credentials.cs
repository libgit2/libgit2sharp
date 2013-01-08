namespace LibGit2Sharp
{
    /// <summary>
    /// Class that holds credentials for remote repository access.
    /// </summary>
    public class Credentials
    {
        /// <summary>
        /// Username for username/password authentication (as in HTTP basic auth).
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password for username/password authentication (as in HTTP basic auth).
        /// </summary>
        public string Password { get; set; }
    }
}
