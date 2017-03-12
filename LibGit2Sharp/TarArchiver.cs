using System;
using System.Globalization;
using System.IO;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Logic for tar archiving (not the actual tar format, but the overal logic related to tar+git) is taken
    /// from https://github.com/git/git/blob/master/archive-tar.c.
    /// </summary>
    internal class TarArchiver : ArchiverBase, IDisposable
    {
        private readonly TarWriter writer;

        public TarArchiver(FileStream output)
        {
            writer = new TarWriter(output);
        }

        #region Overrides of ArchiverBase

        public override void BeforeArchiving(Tree tree, ObjectId oid, DateTimeOffset modificationTime)
        {
            if (oid == null)
            {
                return;
            }

            // Store the sha in the pax_global_header
            using (var stream =
                new MemoryStream(Encoding.ASCII.GetBytes(string.Format(CultureInfo.InvariantCulture,
                                                                       "52 comment={0}\n",
                                                                       oid.Sha))))
            {
                writer.Write("pax_global_header",
                             stream, modificationTime,
                             "666".OctalToInt32(),
                             "0",
                             "0",
                             'g',
                             "root",
                             "root",
                             "0",
                             "0",
                             oid.Sha,
                             false);
            }
        }

        protected override void AddTreeEntry(string path, TreeEntry entry, DateTimeOffset modificationTime)
        {
            switch (entry.Mode)
            {
                case Mode.GitLink:
                case Mode.Directory:
                    writer.Write(path + "/",
                                 null,
                                 modificationTime,
                                 "775".OctalToInt32(),
                                 "0",
                                 "0",
                                 '5',
                                 "root",
                                 "root",
                                 "0",
                                 "0",
                                 entry.TargetId.Sha,
                                 false);
                    break;
                case Mode.ExecutableFile:
                case Mode.NonExecutableFile:
                case Mode.NonExecutableGroupWritableFile:
                    var blob = ((Blob)entry.Target);

                    WriteStream(path,
                                entry,
                                modificationTime,
                                () => blob.IsBinary
                                    ? blob.GetContentStream()
                                    : blob.GetContentStream(new FilteringOptions(path)));
                    break;
                case Mode.SymbolicLink:
                    using (Stream contentStream = ((Blob)entry.Target).GetContentStream(new FilteringOptions(path)))
                    {
                        writer.Write(path,
                                     contentStream,
                                     modificationTime,
                                     "777".OctalToInt32(),
                                     "0",
                                     "0",
                                     '2',
                                     "root",
                                     "root",
                                     "0",
                                     "0",
                                     entry.TargetId.Sha,
                                     true);
                    }
                    break;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                      "Unsupported file mode: {0} (sha1: {1}).",
                                                                      entry.Mode,
                                                                      entry.TargetId.Sha));
            }
        }

        private void WriteStream(string path, TreeEntry entry, DateTimeOffset modificationTime, Func<Stream> streamer)
        {
            using (Stream contentStream = streamer())
            {
                writer.Write(path,
                             contentStream,
                             modificationTime,
                             (entry.Mode == Mode.ExecutableFile)
                                 ? "775".OctalToInt32()
                                 : "664".OctalToInt32(),
                             "0",
                             "0",
                             '0',
                             "root",
                             "root",
                             "0",
                             "0",
                             entry.TargetId.Sha,
                             false);
            }
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            writer.Dispose();
        }

        #endregion
    }
}
