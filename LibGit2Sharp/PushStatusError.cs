namespace LibGit2Sharp
{
    /// <summary>
    /// Information on an error updating reference on remote during a push.
    /// </summary>
    public class PushStatusError
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected PushStatusError()
        { }

        /// <summary>
        /// The reference this status refers to.
        /// </summary>
        public virtual string Reference { get; private set; }

        /// <summary>
        /// The message regarding the update of this reference.
        /// </summary>
        public virtual string Message { get; private set; }

        internal PushStatusError(string reference, string message)
        {
            Reference = reference;
            Message = message;
        }
    }
}
