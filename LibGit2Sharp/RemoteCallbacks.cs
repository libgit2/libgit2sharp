using LibGit2Sharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Exposed git_remote_callbacks callbacks as events
    /// </summary>
    public class RemoteCallbacks
    {
        /// <summary>
        ///   Constructor.
        /// </summary>
        public RemoteCallbacks()
        {
            GitRemoteCallbacks callbacks = new GitRemoteCallbacks();
            callbacks.progress = ProgressChangedHandler;
            callbacks.completion = CompletionChangedHandler;
            callbacks.update_tips = UpdateTipsChangedHandler;
            GitCallbacks = callbacks;
        }

        #region Events

        /// <summary>
        /// Event raised in response to git_remote_callbacks.progress callback.
        /// </summary>
        public EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Event raised in response to git_remote_callbacks.completion callback.
        /// </summary>
        public EventHandler<CompletionChangedEventArgs> CompletionChanged;

        /// <summary>
        /// Event raised in response to git_remote_callbacks.update_tips callback.
        /// </summary>
        public EventHandler<UpdateTipsChangedEventArgs> UpdateTipsChanged;

        #endregion

        internal GitRemoteCallbacks GitCallbacks;

        #region Handlers to respond to callbacks raised by libgit2

        internal void ProgressChangedHandler(IntPtr str, int len, IntPtr data)
        {
            EventHandler<ProgressChangedEventArgs> eh = ProgressChanged;

            if (eh != null)
            {
                string message = Utf8Marshaler.FromNative(str, (uint) len);
                eh(this, new ProgressChangedEventArgs(message));
            }
        }

        internal int CompletionChangedHandler(int type, IntPtr data)
        {
            EventHandler<CompletionChangedEventArgs> eh = CompletionChanged;
            if (eh != null)
            {
                // fire event
            }

            return 0;
        }

        internal int UpdateTipsChangedHandler(IntPtr str, ref GitOid oldId, ref GitOid newId, IntPtr data)
        {
            EventHandler<UpdateTipsChangedEventArgs> eh = UpdateTipsChanged;
            if (eh != null)
            {
                string refName = Utf8Marshaler.FromNative(str);
                eh(this, new UpdateTipsChangedEventArgs(refName, new ObjectId(oldId), new ObjectId(newId)));
            }

            return 0;
        }

        #endregion
    }

    /// <summary>
    ///   Event args containing information on a remote progress.
    ///   Raised in response to git_remote_callbacks.progress callback.
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        /// <summary>
        ///   Constructor.
        /// </summary>
        protected ProgressChangedEventArgs()
        {
            Message = string.Empty;
        }

        internal ProgressChangedEventArgs(string message)
        {
            Message = message;
        }

        /// <summary>
        ///   Message contained in the git_remote_callbacks.progress callback.
        /// </summary>
        public virtual string Message { get; private set; }
    }

    /// <summary>
    ///   Event args containing information on a remote completion.
    ///   Raised in response to git_remote_callbacks.completion callback.
    /// </summary>
    public class CompletionChangedEventArgs : EventArgs
    {
    }

    /// <summary>
    ///   Event args containing information on the updated reference tip.
    ///   Raised in response to git_remote_callbacks.update_tips callback.
    /// </summary>
    public class UpdateTipsChangedEventArgs : EventArgs
    {
        /// <summary>
        ///   Constructor.
        /// </summary>
        protected UpdateTipsChangedEventArgs()
        { }

        internal UpdateTipsChangedEventArgs(string name, ObjectId oldId, ObjectId newId)
        {
            ReferenceName = name;
            OldId = oldId;
            NewId = newId;
        }

        /// <summary>
        /// The name of the reference being updated.
        /// </summary>
        public virtual string ReferenceName { get; private set; }

        /// <summary>
        ///   The old ID of the reference.
        /// </summary>
        public virtual ObjectId OldId { get; private set; }

        /// <summary>
        ///   The new ID of the reference.
        /// </summary>
        public virtual ObjectId NewId { get; private set; }
    }
}
