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
	unsafe public class Index : IEnumerable<IndexEntry>, IDisposable
	{
		private git_index *index = null;

		internal Index(git_index *index)
		{
			this.index = index;
		}

		public Index(string indexPath)
		{
			int ret = NativeMethods.git_index_open_bare(ref index, indexPath);
			GitError.Check(ret);
		}

		public uint Count
		{
			get {
				return NativeMethods.git_index_entrycount(index);
			}
		}

		public void Read()
		{
			NativeMethods.git_index_read(index);
		}

		public void Write()
		{
			NativeMethods.git_index_write(index);
		}

		public Repository Repository
		{
			get {
				if (index->repository == null)
					return null;

				return new Repository(index->repository);
			}
		}

		public IndexEntry Get(uint n)
		{
			git_index_entry *entry = NativeMethods.git_index_get(index, (int)n);

			if (entry == null)
				return null;

			return new IndexEntry(entry);
		}

		public IndexEntry this[uint index]
		{
			get {
				return Get(index);
			}
		}

		public int Get(string path)
		{
			return NativeMethods.git_index_find(index, path);
		}

		public int this[string path]
		{
			get {
				return Get(path);
			}
		}

		public string FilePath
		{
			get {
				return new string(index->index_file_path);
			}
		}

		public void Clear()
		{
			NativeMethods.git_index_clear(index);
		}

		public void Add(string path, int stage)
		{
			int ret = NativeMethods.git_index_add(index, path, stage);
			GitError.Check(ret);
		}

		public void Remove(int position)
		{
			int ret = NativeMethods.git_index_remove(index, position);
			GitError.Check(ret);
		}

		public void Insert(IndexEntry indexEntry)
		{
			int ret = NativeMethods.git_index_insert(index, indexEntry.index_entry);
			GitError.Check(ret);
		}

		#region IEnumerable implementation
		public IEnumerator<IndexEntry> GetEnumerator()
		{
			uint count = Count;
			for (uint i = 0; i < count; i++)
			{
				yield return Get(i);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)this.GetEnumerator();
		}
		#endregion

		#region IDisposable implementation
		public void Dispose()
		{
			if (index != null)
				NativeMethods.git_index_free(index);
		}
		#endregion
	}
}