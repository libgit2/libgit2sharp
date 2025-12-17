using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A note, attached to a given <see cref="GitObject"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Note : IEquatable<Note>
    {
        private static readonly LambdaEqualityHelper<Note> equalityHelper =
            new LambdaEqualityHelper<Note>(x => x.BlobId, x => x.TargetObjectId, x => x.Namespace);

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Note()
        { }

        private Note(ObjectId blobId, string message, ObjectId targetObjectId, string @namespace)
        {
            BlobId = blobId;
            Namespace = @namespace;
            Message = message;
            TargetObjectId = targetObjectId;
        }

        /// <summary>
        /// The <see cref="ObjectId"/> of the blob containing the note message.
        /// </summary>
        public virtual ObjectId BlobId { get; private set; }

        /// <summary>
        /// The message.
        /// </summary>
        public virtual string Message { get; private set; }

        /// <summary>
        /// The namespace with which this note is associated.
        /// <para>This is the abbreviated namespace (e.g.: commits), and not the canonical namespace (e.g.: refs/notes/commits).</para>
        /// </summary>
        public virtual string Namespace { get; private set; }

        /// <summary>
        /// The <see cref="ObjectId"/> of the target object.
        /// </summary>
        public virtual ObjectId TargetObjectId { get; private set; }

        internal static Note BuildFromPtr(NoteHandle note, string @namespace, ObjectId targetObjectId)
        {
            ObjectId oid = Proxy.git_note_id(note);
            string message = Proxy.git_note_message(note);

            return new Note(oid, message, targetObjectId, @namespace);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="Note"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="Note"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="Note"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Note);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Note"/> is equal to the current <see cref="Note"/>.
        /// </summary>
        /// <param name="other">The <see cref="Note"/> to compare with the current <see cref="Note"/>.</param>
        /// <returns>True if the specified <see cref="Note"/> is equal to the current <see cref="Note"/>; otherwise, false.</returns>
        public bool Equals(Note other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Note"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Note"/> to compare.</param>
        /// <param name="right">Second <see cref="Note"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Note left, Note right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Note"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Note"/> to compare.</param>
        /// <param name="right">Second <see cref="Note"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Note left, Note right)
        {
            return !Equals(left, right);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "Target \"{0}\", Namespace \"{1}\": {2}",
                                     TargetObjectId.ToString(7),
                                     Namespace,
                                     Message);
            }
        }
    }
}
