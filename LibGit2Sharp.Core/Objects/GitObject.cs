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
	unsafe public class GitObject
	{
		internal git_object *obj = null;

		internal GitObject(git_object *obj)
		{
			this.obj = obj;
		}

		internal GitObject(Repository repository, git_otype type)
		{
			int ret = NativeMethods.git_object_new(ref obj, repository.repository, type);
			GitError.Check(ret);
		}

		public ObjectId ObjectId
		{
			get {
				return new ObjectId(NativeMethods.git_object_id(obj));
			}
		}

		public git_otype Type
		{
			get {
				return NativeMethods.git_object_type(obj);
			}
		}

		public string StringType
		{
			get {
				return GetType(Type);
			}
		}

		public Repository Owner
		{
			get {
				return new Repository(NativeMethods.git_object_owner(obj));
			}
		}

		public void Write()
		{
			int ret = NativeMethods.git_object_write(obj);
			Console.WriteLine (ret);
		}

		public static string GetType(git_otype type)
		{
			return new string(NativeMethods.git_object_type2string(type));
		}

		internal static git_otype GetType(string str)
		{
			return NativeMethods.git_object_string2type(str);
		}

		internal static git_otype GetType(Type type)
		{
			if (type == typeof(Blob))
				return git_otype.GIT_OBJ_BLOB;
			else if (type == typeof(Commit))
				return git_otype.GIT_OBJ_COMMIT;
			else if (type == typeof(Tree))
				return git_otype.GIT_OBJ_TREE;
			else if (type == typeof(Tag))
				return git_otype.GIT_OBJ_TREE;
			else
				return git_otype.GIT_OBJ_ANY;
		}

		internal static Type GetClass(git_otype type)
		{
			switch (type)
			{
			case git_otype.GIT_OBJ_BLOB:
				return typeof(Blob);
			case git_otype.GIT_OBJ_COMMIT:
				return typeof(Commit);
			case git_otype.GIT_OBJ_TREE:
				return typeof(Tree);
			case git_otype.GIT_OBJ_TAG:
				return typeof(Tag);
			default:
				return null;
			}
		}

		public static Type GetClass(string str)
		{
			return GetClass(GetType(str));
		}

		internal static GitObject Create(git_object *obj)
		{
			switch (NativeMethods.git_object_type(obj))
			{
			case git_otype.GIT_OBJ_BAD:
				throw new ArgumentException("The object must not be a bad object");
			case git_otype.GIT_OBJ_BLOB:
				return new Blob(obj);
			case git_otype.GIT_OBJ_COMMIT:
				return new Commit(obj);
			case git_otype.GIT_OBJ_TREE:
				return new Tree(obj);
			case git_otype.GIT_OBJ_TAG:
				return new Tag(obj);
			default:
				return new GitObject(obj);
			}
		}

		internal static T Create<T>(git_object *obj) where T : GitObject
		{
			git_otype type = NativeMethods.git_object_type(obj);

			// If class doesn't match type, return the default
			if (typeof(T) != GetClass(type))
				return default(T);

			return (T)Create(obj);
		}
	}
}