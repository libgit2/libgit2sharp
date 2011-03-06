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

namespace Git2
{
	unsafe public class RevisionWalker : IDisposable
	{
		internal git_revwalk *revwalk;
		public RevisionWalker(Repository repository)
		{
			int ret = NativeMethods.git_revwalk_new(ref revwalk, repository.repository);
			GitError.Check(ret);
		}

		public void Push(Commit commit)
		{
			int ret = NativeMethods.git_revwalk_push(revwalk, commit.commit);
			GitError.Check(ret);
		}

		public void Hide(Commit commit)
		{
			int ret = NativeMethods.git_revwalk_hide(revwalk, commit.commit);
			GitError.Check(ret);
		}

		public Commit Next()
		{
			git_commit *commit = null;
			int ret = NativeMethods.git_revwalk_next(ref commit, revwalk);
			if (ret == -21)
			{
				return null;
			}
			GitError.Check(ret);
			return (Commit)GitObject.Create((git_object *)commit);
		}

		public void Sorting(uint sortMode)
		{
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (revwalk != null)
				NativeMethods.git_revwalk_free(revwalk);
		}
		#endregion
	}
}

