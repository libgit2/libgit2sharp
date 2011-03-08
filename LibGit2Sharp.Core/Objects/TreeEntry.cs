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
	unsafe public class TreeEntry
	{
		internal git_tree_entry *entry;

		internal TreeEntry(git_tree_entry *entry)
		{
			this.entry = entry;
		}

		public ObjectId ObjectId
		{
			get {
				return new ObjectId(NativeMethods.git_tree_entry_id(entry));
			}
			set {
				NativeMethods.git_tree_entry_set_id(entry, &value.oid);
			}
		}

		public string Filename
		{
			get {
				return new string(NativeMethods.git_tree_entry_name(entry));
			}
			set {
				NativeMethods.git_tree_entry_set_name(entry, value);
			}
		}

		public uint Attributes
		{
			get {
				return NativeMethods.git_tree_entry_attributes(entry);
			}
			set {
				NativeMethods.git_tree_entry_set_attributes(entry, (int)value);
			}
		}

		public GitObject GetObject()
		{
			git_object *obj = null;
			NativeMethods.git_tree_entry_2object(&obj, entry);

			if (obj == null)
				return null;

			return GitObject.Create(obj);
		}

		public T Get<T>() where T : GitObject
		{
			git_object *obj = null;
			NativeMethods.git_tree_entry_2object(&obj, entry);

			if (obj == null)
				return default(T);

			return GitObject.Create<T>(obj);
		}
	}
}