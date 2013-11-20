using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="NoteCollection"/>.
    /// </summary>
    public static class NoteCollectionExtensions
    {
        /// <summary>
        /// Creates or updates a <see cref="Note"/> on the specified object, and for the given namespace.
        /// <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="collection">The <see cref="NoteCollection"/></param>
        /// <param name="targetId">The target <see cref="ObjectId"/>, for which the note will be created.</param>
        /// <param name="message">The note message.</param>
        /// <param name="namespace">The namespace on which the note will be created. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        /// <returns>The note which was just saved.</returns>
        public static Note Add(this NoteCollection collection, ObjectId targetId, string message, string @namespace)
        {
            Signature author = collection.repo.Config.BuildSignature(DateTimeOffset.Now, true);

            return collection.Add(targetId, message, author, author, @namespace);
        }

        /// <summary>
        /// Deletes the note on the specified object, and for the given namespace.
        /// <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="collection">The <see cref="NoteCollection"/></param>
        /// <param name="targetId">The target <see cref="ObjectId"/>, for which the note will be created.</param>
        /// <param name="namespace">The namespace on which the note will be removed. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        public static void Remove(this NoteCollection collection, ObjectId targetId, string @namespace)
        {
            Signature author = collection.repo.Config.BuildSignature(DateTimeOffset.Now, true);

            collection.Remove(targetId, author, author, @namespace);
        }
    }
}
