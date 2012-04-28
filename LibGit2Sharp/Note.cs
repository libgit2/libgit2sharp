using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    public class Note : GitObject
    {
        private Note(ObjectId id, string message) : base(id)
        {
            Message = message;
        }

        public string Message { get; private set; }

        internal static Note BuildFromPtr(NoteSafeHandle note)
        {
            ObjectId oid = NativeMethods.git_note_oid(note).MarshalAsObjectId();
            var message = NativeMethods.git_note_message(note);

            return new Note(oid, message);
        }
    }
}