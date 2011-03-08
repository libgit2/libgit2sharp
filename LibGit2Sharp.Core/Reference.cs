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
	unsafe abstract public class Reference
	{
		internal git_reference *reference = null;

		internal Reference()
		{
		}

		public ObjectIdReference Resolve()
		{
			git_reference *resolved_ref = null;

			int ret = NativeMethods.git_reference_resolve(ref resolved_ref, reference);
			GitError.Check(ret);

			return Reference.Create(resolved_ref) as ObjectIdReference;
		}

		public Repository Owner
		{
			get {
				return new Repository(NativeMethods.git_reference_owner(reference));
			}
		}

		public void Rename(string newName)
		{
			int ret = NativeMethods.git_reference_rename(reference, newName);
			GitError.Check(ret);
		}

		public void Delete()
		{
			int ret = NativeMethods.git_reference_delete(reference);
			GitError.Check(ret);
		}

		internal static Reference Create(git_reference *reference)
		{
			switch (NativeMethods.git_reference_type(reference))
			{
			case git_rtype.GIT_REF_SYMBOLIC:
				return new SymbolicReference(reference);
			case git_rtype.GIT_REF_OID:
				return new ObjectIdReference(reference);
			case git_rtype.GIT_REF_INVALID:
				throw new InvalidReferenceException();
			default:
				throw new Exception("Reference type not yet implemented");
			}
		}
	}

	unsafe public class SymbolicReference : Reference
	{
		internal SymbolicReference(git_reference *reference)
		{
			this.reference = reference;
		}

		public SymbolicReference(Repository repository, string name, string target)
		{
			int ret = NativeMethods.git_reference_create_symbolic(ref reference, repository.repository, name, target);
			GitError.Check(ret);
			if (reference == null)
				throw new Exception();
		}

		public string Target
		{
			get {
				return new string(NativeMethods.git_reference_target(reference));
			}
			set {
				int ret = NativeMethods.git_reference_set_target(reference, value);
				GitError.Check(ret);
			}
		}
	}

	unsafe public class ObjectIdReference : Reference
	{
		internal ObjectIdReference(git_reference *reference)
		{
			this.reference = reference;
		}

		public ObjectIdReference(Repository repository, string name, ObjectId oid)
		{
			fixed (git_reference **reference = &this.reference)
			{
				int ret = NativeMethods.git_reference_create_oid(reference, repository.repository, name, &oid.oid);
				GitError.Check(ret);
			}
		}

		public ObjectId ObjectId
		{
			get {
				return new ObjectId(NativeMethods.git_reference_oid(reference));
			}
			set {
				NativeMethods.git_reference_set_oid(reference, &value.oid);
			}
		}
	}
}