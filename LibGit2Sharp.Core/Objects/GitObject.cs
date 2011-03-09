/*
 * The MIT License
 *
 * Copyright (c) 2011 Andrius Bentkus
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
			Pointer = new IntPtr(obj);
		}

		internal GitObject(Repository repository, git_otype type)
		{
			int ret;
			
			fixed (git_object **obj = &this.obj)
			{
				ret = NativeMethods.git_object_new(obj, repository.repository, type);
			}
			GitError.Check(ret);
		}
		
		public IntPtr Pointer { get; protected set; }

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
