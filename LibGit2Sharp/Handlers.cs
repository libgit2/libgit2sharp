using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp.Handlers
{
    /// <summary>
    ///   Delegate definition to handle Progress callback. 
    ///   Returns the text as reported by the server. The text
    ///   in the serverProgressOutput parameter is not delivered
    ///   in any particular units (i.e. not necessarily delivered
    ///   as whole lines) and is likely to be chunked as partial lines.
    /// </summary>
    /// <param name="serverProgressOutput">text reported by the server. 
    ///   Text can be chunked at arbitrary increments (i.e. can be composed
    ///   of a partial line of text).</param>
    public delegate void ProgressHandler(string serverProgressOutput);

    /// <summary>
    ///   Delegate definition to handle UpdateTips callback.
    /// </summary>
    /// <param name="referenceName">Name of the updated reference.</param>
    /// <param name="oldId">Old ID of the reference.</param>
    /// <param name="newId">New ID of the reference.</param>
    /// <returns>Return negative integer to cancel.</returns>
    public delegate int UpdateTipsHandler(string referenceName, ObjectId oldId, ObjectId newId);

    /// <summary>
    ///   Delegate definition to handle Completion callback.
    /// </summary>
    /// <param name="RemoteCompletionType"></param>
    /// <returns></returns>
    public delegate int CompletionHandler(RemoteCompletionType RemoteCompletionType);

    /// <summary>
    ///   Delegate definition for transfer progress callback.
    /// </summary>
    /// <param name="progress">The <see cref = "TransferProgress" /> object containing progress information.</param>
    public delegate void TransferProgressHandler(TransferProgress progress);

    /// <summary>
    ///   Delegate definition for checkout progress callback.
    /// </summary>
    /// <param name="path">Path of the updated file.</param>
    /// <param name="completedSteps">Number of completed steps.</param>
    /// <param name="totalSteps">Total number of steps.</param>
    public delegate void CheckoutProgressHandler(string path, int completedSteps, int totalSteps);
}
