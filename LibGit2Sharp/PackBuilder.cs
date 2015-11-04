using System;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Representation of a git PackBuilder.
    /// </summary>
    public sealed class PackBuilder : IDisposable
    {
        private readonly PackBuilderSafeHandle packBuilderHandle;

        /// <summary>
        /// Constructs a PackBuilder for a <see cref="Repository"/>.
        /// </summary>
        internal PackBuilder(Repository repository)
        {
            Ensure.ArgumentNotNull(repository, "repository");

            packBuilderHandle = Proxy.git_packbuilder_new(repository.Handle);
        }

        /// <summary>
        /// Inserts a single <see cref="GitObject"/> to the PackBuilder.
        /// For an optimal pack it's mandatory to insert objects in recency order, commits followed by trees and blobs. (quoted from libgit2 API ref)
        /// </summary>
        /// <param name="gitObject">The object to be inserted.</param>
        /// <exception cref="ArgumentNullException">if the gitObject is null</exception>
        public void Add<T>(T gitObject) where T : GitObject
        {
            Ensure.ArgumentNotNull(gitObject, "gitObject");

            Add(gitObject.Id);
        }

        /// <summary>
        /// Recursively inserts a <see cref="GitObject"/> and its referenced objects.
        /// Inserts the object as well as any object it references.
        /// </summary>
        /// <param name="gitObject">The object to be inserted recursively.</param>
        /// <exception cref="ArgumentNullException">if the gitObject is null</exception>
        public void AddRecursively<T>(T gitObject) where T : GitObject
        {
            Ensure.ArgumentNotNull(gitObject, "gitObject");

            AddRecursively(gitObject.Id);
        }

        /// <summary>
        /// Inserts a single object to the PackBuilder by its <see cref="ObjectId"/>.
        /// For an optimal pack it's mandatory to insert objects in recency order, commits followed by trees and blobs. (quoted from libgit2 API ref)
        /// </summary>
        /// <param name="id">The object ID to be inserted.</param>
        /// <exception cref="ArgumentNullException">if the id is null</exception>
        public void Add(ObjectId id)
        {
            Ensure.ArgumentNotNull(id, "id");

            Proxy.git_packbuilder_insert(packBuilderHandle, id, null);
        }

        /// <summary>
        /// Recursively inserts an object and its referenced objects by its <see cref="ObjectId"/>.
        /// Inserts the object as well as any object it references.
        /// </summary>
        /// <param name="id">The object ID to be recursively inserted.</param>
        /// <exception cref="ArgumentNullException">if the id is null</exception>
        public void AddRecursively(ObjectId id)
        {
            Ensure.ArgumentNotNull(id, "id");

            Proxy.git_packbuilder_insert_recur(packBuilderHandle, id, null);
        }

        /// <summary>
        /// Disposes the PackBuilder object.
        /// </summary>
        void IDisposable.Dispose()
        {
            packBuilderHandle.SafeDispose();
        }

        /// <summary>
        /// Writes the pack file and corresponding index file to path.
        /// </summary>
        /// <param name="path">The path that pack and index files will be written to it.</param>
        internal void Write(string path)
        {
            Proxy.git_packbuilder_write(packBuilderHandle, path);
        }

        /// <summary>
        /// Write the new pack to the <paramref name="outputStream"/>. The index will not be
        /// written, so the caller is responsible for indexing objects in the new pack.
        /// </summary>
        /// <param name="outputStream">The output pack stream.</param>
        internal void WriteTo(Stream outputStream)
        {
            Ensure.ArgumentNotNull(outputStream, "outputStream");

            // 64K is optimal buffer size per https://technet.microsoft.com/en-us/library/cc938632.aspx
            const int defaultBufferSize = 64 * 1024;

            Proxy.git_packbuilder_foreach(packBuilderHandle, (IntPtr buf, UIntPtr size, IntPtr payload) =>
            {
                try
                {
                    long objectSize = (long)size;
                    unsafe
                    {
                        using (var input = new UnmanagedMemoryStream((byte*)buf.ToPointer(), objectSize))
                        {
                            input.CopyTo(
                                outputStream,
                                objectSize > defaultBufferSize ? defaultBufferSize : (int)objectSize);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(LogLevel.Error, "PackBuilder.WriteTo callback exception");
                    Log.Write(LogLevel.Error, ex.ToString());
                    Proxy.giterr_set_str(GitErrorCategory.Callback, ex);
                    return (int)GitErrorCode.Error;
                }

                return (int)GitErrorCode.Ok;
            });
        }

        /// <summary>
        /// Sets number of threads to spawn.
        /// </summary>
        /// <returns> Returns the number of actual threads to be used.</returns>
        /// <param name="nThread">The Number of threads to spawn. An argument of 0 ensures using all available CPUs</param>
        internal int SetMaximumNumberOfThreads(int nThread)
        {
            // Libgit2 set the number of threads to 1 by default, 0 ensures git_online_cpus
            return (int)Proxy.git_packbuilder_set_threads(packBuilderHandle, (uint)nThread);
        }

        /// <summary>
        /// Number of objects the PackBuilder will write out.
        /// </summary>
        internal long ObjectsCount
        {
            get { return Proxy.git_packbuilder_object_count(packBuilderHandle); }
        }

        /// <summary>
        /// Number of objects the PackBuilder has already written out. 
        /// This is only correct after the pack file has been written.
        /// </summary>
        internal long WrittenObjectsCount
        {
            get { return Proxy.git_packbuilder_written(packBuilderHandle); }
        }

        internal PackBuilderSafeHandle Handle
        {
            get { return packBuilderHandle; }
        }
    }

    /// <summary>
    /// The results of pack process of the <see cref="ObjectDatabase"/>.
    /// </summary>
    public struct PackBuilderResults
    {
        /// <summary>
        /// Number of objects the PackBuilder has already written out. 
        /// </summary>
        public long WrittenObjectsCount { get; internal set; }
    }

    /// <summary>
    /// Packing options of the <see cref="ObjectDatabase"/>.
    /// </summary>
    public sealed class PackBuilderOptions
    {
        private int nThreads;
        private Action<PackBuilder> packDelegate;

        /// <summary>
        /// Maximum number of threads to spawn.
        /// The default value is 0 which ensures using all available CPUs.
        /// </summary>
        public int MaximumNumberOfThreads
        {
            set
            {
                Ensure.ArgumentPositiveInt32(value, "value");
                nThreads = value;
            }
            get
            {
                return nThreads;
            }
        }

        /// <summary>
        /// Packing action to perform instead of the default, which is to
        /// pack all objects in the repository into a single packfile and index,
        /// enumerating them in an arbitrary order.
        /// </summary>
        public Action<PackBuilder> PackDelegate
        {
            set
            {
                Ensure.ArgumentNotNull(value, "value");
                packDelegate = value;
            }
            get
            {
                return packDelegate;
            }
        }
    }
}
