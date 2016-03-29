namespace LibGit2Sharp
{
    /// <summary>
    /// Can be used to reference the <see cref="IRepository" /> from which
    /// an instance was created.
    /// <para>
    /// While convenient in some situations (e.g. Checkout branch bound to UI element),
    /// it is important to ensure instances created from an <see cref="IRepository" />
    /// are not used after it is disposed.
    /// </para>
    /// <para>
    /// It's generally better to create <see cref="IRepository" /> and dependant instances
    /// on demand, with a short lifespan.
    /// </para>
    /// </summary>
    public interface IBelongToARepository
    {
        /// <summary>
        /// The <see cref="IRepository" /> from which this instance was created.
        /// <para>
        /// The returned value should not be disposed.
        /// </para>
        /// </summary>
        IRepository Repository { get; }
    }
}
