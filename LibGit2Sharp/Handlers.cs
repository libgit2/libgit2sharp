using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace LibGit2Sharp.Handlers
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
    /// Delegate definition for the credentials retrieval callback
    /// </summary>
    /// <param name="url">The url</param>
    /// <param name="usernameFromUrl">Username which was extracted from the url, if any</param>
    /// <param name="types">Credential types which the server accepts</param>
    public delegate Credentials CredentialsHandler(string url, string usernameFromUrl, SupportedCredentialTypes types);

    /// <summary>
    /// Delegate definition for the certificate validation
    /// </summary>
    /// <param name="certificate">The certificate which the server sent</param>
    /// <param name="host">The hostname which we tried to connect to</param>
    /// <param name="valid">Whether libgit2 thinks this certificate is valid</param>
    /// <returns>True to continue, false to cancel</returns>
    public delegate bool CertificateCheckHandler(Certificate certificate, bool valid, string host);

    /// <summary>
    /// Delegate definition for transfer progress callback.
    /// </summary>
    /// <param name="progress">The <see cref="TransferProgress"/> object containing progress information.</param>
    /// <returns>True to continue, false to cancel.</returns>
    public delegate bool TransferProgressHandler(TransferProgress progress);

    /// <summary>
    /// Delegate definition to indicate that a repository is about to be operated on.
    /// (In the context of a recursive operation).
    /// </summary>
    /// <param name="context">Context on the repository that is being operated on.</param>
    /// <returns>true to continue, false to cancel.</returns>
    public delegate bool RepositoryOperationStarting(RepositoryOperationContext context);

    /// <summary>
    /// Delegate definition to indicate that an operation is done in a repository.
    /// (In the context of a recursive operation).
    /// </summary>
    /// <param name="context">Context on the repository that is being operated on.</param>
    public delegate void RepositoryOperationCompleted(RepositoryOperationContext context);

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
    /// Provides information about what updates will be performed before a push occurs
    /// </summary>
    /// <param name="updates">List of updates about to be performed via push</param>
    /// <returns>True to continue, false to cancel.</returns>
    public delegate bool PrePushHandler(IEnumerable<PushUpdate> updates);

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
    /// Delegate definition for remote rename failure callback.
    /// <para>
    ///   This callback will be called to notify the caller of fetch refspecs
    ///   that haven't been automatically updated and need potential manual tweaking.
    /// </para>
    /// </summary>
    /// <param name="problematicRefspec">The refspec which didn't match the default.</param>
    public delegate void RemoteRenameFailureHandler(string problematicRefspec);

    /// <summary>
    /// Delegate definition for stash application callback.
    /// </summary>
    /// <param name="progress">The current step of the stash application.</param>
    /// <returns>True to continue checkout operation; false to cancel checkout operation.</returns>
    public delegate bool StashApplyProgressHandler(StashApplyProgress progress);

    /// <summary>
    /// Delegate to report information on a rebase step that is about to be performed.
    /// </summary>
    /// <param name="beforeRebaseStep"></param>
    public delegate void RebaseStepStartingHandler(BeforeRebaseStepInfo beforeRebaseStep);

    /// <summary>
    /// Delegate to report information on the rebase step that was just completed.
    /// </summary>
    /// <param name="afterRebaseStepInfo"></param>
    public delegate void RebaseStepCompletedHandler(AfterRebaseStepInfo afterRebaseStepInfo);

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

    /// <summary>
    /// Delegate definition for logging.  This callback will be used to
    /// write logging messages in libgit2 and LibGit2Sharp.
    /// </summary>
    /// <param name="level">The level of the log message.</param>
    /// <param name="message">The log message.</param>
    public delegate void LogHandler(LogLevel level, string message);
}
