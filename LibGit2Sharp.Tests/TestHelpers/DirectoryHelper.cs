﻿using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class DirectoryHelper
    {
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            // From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c/58779#58779

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }
            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name));
            }
        }

        public static void DeleteSubdirectories(string parentPath)
        {
            string[] dirs = Directory.GetDirectories(parentPath);
            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }
        }

        public static void DeleteDirectory(string directoryPath)
        {
            // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

            string[] files = Directory.GetFiles(directoryPath);
            string[] dirs = Directory.GetDirectories(directoryPath);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
            try
            {
                Directory.Delete(directoryPath, false);
            } 
            catch (IOException ex)
            {
                throw new IOException(string.Format("{0}The directory '{1}' could not be deleted!" +
                                                    "{0}Most of the time, this is due to an external process accessing the files in the temporary repositories created during the test runs, and keeping a handle on the directory, thus preventing the deletion of those files." +
                                                    "{0}Known and common causes include:" +
                                                    "{0}- Windows Search Indexer (go to the Indexing Options, in the Windows Control Panel, and exclude the bin folder of LibGit2Sharp.Tests)" +
                                                    "{0}- Antivirus (exclude the bin folder of LibGit2Sharp.Tests from the paths scanned by your real-time antivirus){0}", 
                    Environment.NewLine, Path.GetFullPath(directoryPath)), ex);
            }
        }
    }
}
