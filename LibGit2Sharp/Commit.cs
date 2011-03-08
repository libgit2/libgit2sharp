/*
 * This file is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License, version 2,
 * as published by the Free Software Foundation.
 *
 * In addition to the permissions in the GNU General Public License,
 * the authors give you unlimited permission to link the compiled
 * version of this file into combinations with other programs,
 * and to distribute those combinations without any restriction
 * coming from the use of this file.  (The General Public License
 * restrictions do apply in other respects; for example, they cover
 * modification of the file, and distribution when not linked into
 * a combined executable.)
 *
 * This file is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
 */

using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    public class Commit : GitObject
    {
        public IEnumerable<GitObject> Parents { get; private set; }
        public Signature Author { get; private set; }
        public Signature Committer { get; private set; }
        public DateTimeOffset When { get; private set; }
        public string Message { get; private set; }
        public string MessageShort { get; private set; }
        public Tree Tree { get; private set; }

        public Commit(string objectId, Signature author, Signature committer, string message, string messageShort, Tree tree, IEnumerable<GitObject> parents)
            : base(objectId, ObjectType.Commit)
        {
            Parents = parents;
            Author = author;
            Committer = committer;
            When = committer.When;
            Message = message;
            MessageShort = messageShort;
            Tree = tree;
        }
    }
}
