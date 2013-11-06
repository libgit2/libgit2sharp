﻿namespace LibGit2Sharp.Handlers
{
    /// <summary>
    /// Delegate definition to handle Progress callback.
    /// Returns the text as reported by the server. The text
    /// in the serverProgressOutput parameter is not delivered
    /// in any particular units (i.e. not necessarily delivered
    /// as whole lines) and is likely to be chunked as partial lines.
    /// </summary>
    /// <param name="serverProgressOutput">text reported by the server.
    /// Text can be chunked at arbitrary increments (i.e. can be composed
    /// of a partial line of text).</param>
    /// <returns>True to continue, false to cancel.</returns>
    public delegate bool ProgressHandler(string serverProgressOutput);

    /// <summary>
    /// Delegate definition to handle UpdateTips callback.
    /// </summary>
    /// <param name="referenceName">Name of the updated reference.</param>
    /// <param name="oldId">Old ID of the reference.</param>
    /// <param name="newId">New ID of the reference.</param>
    /// <returns>True to continue, false to cancel.</returns>
    public delegate bool UpdateTipsHandler(string referenceName, ObjectId oldId, ObjectId newId);

    /// <summary>
    /// Delegate definition for transfer progress callback.
    /// </summary>
    /// <param name="progress">The <see cref="TransferProgress"/> object containing progress information.</param>
    /// <returns>True to continue, false to cancel.</returns>
    public delegate bool TransferProgressHandler(TransferProgress progress);

    /// <summary>
    /// Delegate definition for callback reporting push network progress.
    /// </summary>
    /// <param name="current">The current number of objects sent to server.</param>
    /// <param name="total">The total number of objects to send to the server.</param>
    /// <param name="bytes">The number of bytes sent to the server.</param>
    /// <returns>True to continue, false to cancel.</returns>
    public delegate bool PushTransferProgressHandler(int current, int total, long bytes);

    /// <summary>
    /// Delegate definition for callback reporting pack builder progress.
    /// </summary>
    /// <param name="stage">The current stage progress is being reported for.</param>
    /// <param name="current">The current number of objects processed in this this stage.</param>
    /// <param name="total">The total number of objects to process for the current stage.</param>
    /// <returns>True to continue, false to cancel.</returns>
    public delegate bool PackBuilderProgressHandler(PackBuilderStage stage, int current, int total);

    /// <summary>
    /// Delegate definition to handle reporting errors when updating references on the remote.
    /// </summary>
    /// <param name="pushStatusErrors">The reference name and error from the server.</param>
    public delegate void PushStatusErrorHandler(PushStatusError pushStatusErrors);

    /// <summary>
    /// Delegate definition for checkout progress callback.
    /// </summary>
    /// <param name="path">Path of the updated file.</param>
    /// <param name="completedSteps">Number of completed steps.</param>
    /// <param name="totalSteps">Total number of steps.</param>
    public delegate void CheckoutProgressHandler(string path, int completedSteps, int totalSteps);

    /// <summary>
    /// Delegate definition for checkout notification callback.
    /// </summary>
    /// <param name="path">The path the callback corresponds to.</param>
    /// <param name="notifyFlags">The checkout notification type.</param>
    /// <returns>True to continue checkout operation; false to cancel checkout operation.</returns>
    public delegate bool CheckoutNotifyHandler(string path, CheckoutNotifyFlags notifyFlags);

    /// <summary>
    /// Delegate definition for unmatched path callback.
    /// <para>
    ///   This callback will be called to notify the caller of unmatched path.
    /// </para>
    /// </summary>
    /// <param name="unmatchedPath">The unmatched path.</param>
    public delegate void UnmatchedPathHandler(string unmatchedPath);

    /// <summary>
    /// The stages of pack building.
    /// </summary>
    public enum PackBuilderStage
    {
        /// <summary>
        /// Counting stage.
        /// </summary>
        Counting,

        /// <summary>
        /// Deltafying stage.
        /// </summary>
        Deltafying
    }
}
