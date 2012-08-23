namespace LibGit2Sharp
{
    /// <summary>
    ///   Git specific modes for entries.
    /// </summary>
    public enum Mode
    {
        // Inspired from http://stackoverflow.com/a/8347325/335418

        /// <summary>
        ///   000000 file mode (the entry doesn't exist)
        /// </summary>
        Nonexistent = 0,

        /// <summary>
        ///   040000 file mode
        /// </summary>
        Directory = 0x4000,

        /// <summary>
        ///   100644 file mode
        /// </summary>
        NonExecutableFile = 0x81A4,

        /// <summary>
        ///   Obsolete 100664 file mode.
        ///   <para>0100664 mode is an early Git design mistake. It's kept for
        ///     ascendant compatibility as some <see cref="Tree"/> and
        ///     <see cref="Repository.Index"/> entries may still bear
	    ///     this mode in some old git repositories, but it's now deprecated.
        ///   </para>
        /// </summary>
        NonExecutableGroupWritableFile = 0x81B4,

        /// <summary>
        ///   100755 file mode
        /// </summary>
        ExecutableFile = 0x81ED,

        /// <summary>
        ///   120000 file mode
        /// </summary>
        SymbolicLink = 0xA000,

        /// <summary>
        ///   160000 file mode
        /// </summary>
        GitLink = 0xE000
    }
}
