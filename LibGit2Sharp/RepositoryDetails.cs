﻿/*
 * The MIT License
 *
 * Copyright (c) 2011 Emeric Fermas
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;

namespace LibGit2Sharp
{
    public class RepositoryDetails
    {
        private static readonly Func<string, string> Check = path => String.IsNullOrEmpty(path) ? "Undetermined" : path;

        public RepositoryDetails(string repositoryDirectory, string index, string databaseDirectory, string workingDirectory, bool isBare)
        {
            RepositoryDirectory = Check(repositoryDirectory);
            Index = Check(index);
            DatabaseDirectory = Check(databaseDirectory);
            WorkingDirectory = Check(workingDirectory);

            IsBare = isBare;
        }

        public bool IsBare { get; private set; }
        public string RepositoryDirectory { get; private set; }
        public string Index { get; private set; }
        public string DatabaseDirectory { get; private set; }
        public string WorkingDirectory { get; private set; }
    }
}
