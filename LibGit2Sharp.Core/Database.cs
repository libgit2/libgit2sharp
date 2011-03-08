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
	unsafe public class Database
	{
		internal git_odb *database;
		
		internal Database(git_odb *database)
		{
			this.database = database;
		}
		
		public Database(string objectsDir)
		{
			int ret;
			fixed (git_odb **database = &this.database)
			{
				ret = NativeMethods.git_odb_open(database, objectsDir);
			}
			GitError.Check(ret);
		}
		
		public bool Exists(ObjectId id)
		{
			return (NativeMethods.git_odb_exists(database, &id.oid) > 0);
		}

		public void Close()
		{
			NativeMethods.git_odb_close(database);
		}
		
		public RawObject ReadHeader(ObjectId id)
		{
			git_rawobj ro = new git_rawobj();
			
			int ret = NativeMethods.git_odb_read_header(ref ro, database, &id.oid);
			GitError.Check(ret);
			return new RawObject(ro);
		}
	}
}

