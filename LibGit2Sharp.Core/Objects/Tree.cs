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
using System.Collections;
using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
	unsafe public class Tree : GitObject, IEnumerable<TreeEntry>
	{
		internal git_tree *tree = null;

		internal Tree(git_object *obj)
			: this((git_tree *)obj)
		{
		}

		internal Tree(git_tree *tree)
			: base((git_object *)tree)
		{
			this.tree = tree;
		}

		public Tree(Repository repository)
			: base(repository, git_otype.GIT_OBJ_TREE)
		{
			this.tree = (git_tree *)obj;
		}

		public uint TreeEntryCount
		{
			get {
				return NativeMethods.git_tree_entrycount(tree);
			}
		}

		public TreeEntry Get(uint index)
		{
			return new TreeEntry(NativeMethods.git_tree_entry_byindex(tree, (int)index));
		}

		public TreeEntry Get(string filename)
		{
			return new TreeEntry(NativeMethods.git_tree_entry_byname(tree, filename));
		}

		public TreeEntry this[uint index]
		{
			get {
				return Get(index);
			}
		}

		public TreeEntry this[string filename]
		{
			get {
				return Get(filename);
			}
		}

		public TreeEntry Add(ObjectId oid, string filename, int attributes)
		{
			git_tree_entry *tree_entry = null;
			int ret = NativeMethods.git_tree_add_entry(&tree_entry, tree, &oid.oid, filename, attributes);
			GitError.Check(ret);
			return new TreeEntry(tree_entry);
		}

		public void Remove(int index)
		{
			int ret = NativeMethods.git_tree_remove_entry_byindex(tree, index);
			GitError.Check(ret);
		}

		public void Remove(string str)
		{
			int ret = NativeMethods.git_tree_remove_entry_byname(tree, str);
			GitError.Check(ret);
		}

		public void ClearEntries()
		{
			NativeMethods.git_tree_clear_entries(tree);
		}

		public IEnumerator<TreeEntry> GetEnumerator()
		{
			uint count = TreeEntryCount;
			for (uint i = 0; i < count; i++)
			{
				yield return Get(i);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}
	}
}