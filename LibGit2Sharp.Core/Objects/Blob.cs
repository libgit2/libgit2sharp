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
	unsafe public class Blob : GitObject
	{
		internal git_blob *blob;

		internal Blob(git_object *obj)
			: this((git_blob *)obj)
		{
		}

		internal Blob(git_blob *blob)
			: base((git_object *)blob)
		{
			this.blob = blob;
		}

		public Blob(Repository repository)
			: base(repository, git_otype.GIT_OBJ_BLOB)
		{
			this.blob = (git_blob *)obj;
		}

		public void SetRawContentFromFile(string filename)
		{
			int ret = NativeMethods.git_blob_set_rawcontent_fromfile(blob, filename);
			GitError.Check(ret);
		}

		// TODO: implement a lot of overload methods for this!
		public void SetRawContent()
		{
			throw new NotImplementedException();
		}

		private void *GetRawContent()
		{
			// TODO: this has to be fixed first in the type definitions of libgit2
			// return NativeMethods.git_blob_rawcontent(blob);
			return null;
		}

		public int Size
		{
			get {
				return NativeMethods.git_blob_rawsize(blob);
			}
		}

		public static void WriteFile(ObjectId writtenId, Repository repository, string path)
		{
			fixed (git_oid *poid = &writtenId.oid)
			{
				NativeMethods.git_blob_writefile(poid, repository.repository, path);
			}
		}
	}
}