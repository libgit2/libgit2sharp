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

namespace LibGit2Sharp.Core
{
	unsafe public class Repository : IDisposable
	{
		internal git_repository *repository = null;

		internal Repository(git_repository *repository)
		{
			this.repository = repository;
		}

		public Repository(string path)
		{
			int ret;
			fixed (git_repository **repo = &repository)
			{
				ret = NativeMethods.git_repository_open(repo, path);
			}
			GitError.Check(ret);
		}
		
		public Repository(string gitDir, string objcetDirectory, string indexFile, string workTree)
		{
			int ret;
			fixed (git_repository **repo = &repository)
			{
				ret = NativeMethods.git_repository_open2(repo, gitDir, objcetDirectory, indexFile, workTree);
			}
			GitError.Check(ret);
		}

		public static Repository Init(string path, bool bare)
		{
			git_repository *repo = null;

			int ret = NativeMethods.git_repository_init(&repo, path, (uint)(bare ? 1 : 0));
			GitError.Check(ret);

			return new Repository(repo);
		}

		public static Repository Init(string path)
		{
			return Init(path, false);
		}

		public Index Index
		{
			get {
				git_index *index = null;
				int ret = NativeMethods.git_repository_index(&index, repository);
				GitError.Check(ret);
				NativeMethods.git_index_read(index);
				return new Index(index);
			}
		}

		public GitObject Lookup(ObjectId oid)
		{
			return Lookup(oid, git_otype.GIT_OBJ_ANY);
		}

		public GitObject Lookup(ObjectId oid, git_otype type)
		{
			git_object *obj = null;
			int ret = NativeMethods.git_object_lookup(&obj, repository, &oid.oid, type);
			
			GitError.Check(ret);

			if (obj == null)
				return null;

			return GitObject.Create(obj);
		}

		public T Lookup<T>(ObjectId oid) where T : GitObject
		{
			git_object *obj = null;
			int ret = NativeMethods.git_object_lookup(&obj, repository, &oid.oid, GitObject.GetType(typeof(T)));
			GitError.Check(ret);

			if (obj == null)
				return default(T);

			return GitObject.Create<T>(obj);
		}

		public Reference ReferenceLookup(string name)
		{
			git_reference *reference = null;

			int ret = NativeMethods.git_reference_lookup(&reference, repository, name);
			GitError.Check(ret);

			if (reference == null)
				return null;

			return Reference.Create(reference);
		}

		public Index OpenIndex()
		{
			git_index *index = null;
			int ret = NativeMethods.git_index_open_inrepo(&index, repository);
			GitError.Check(ret);

			if (index == null)
				return null;

			return new Index(index);
		}

		public void WriteFile(ObjectId writtenId, string path)
		{
			Blob.WriteFile(writtenId, this, path);
		}

		#region IDisposable implementation
		public void Dispose()
		{
			NativeMethods.git_repository_free(repository);
		}
		#endregion
	}
}