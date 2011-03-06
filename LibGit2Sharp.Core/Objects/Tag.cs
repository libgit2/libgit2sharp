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
	unsafe public class Tag : GitObject
	{
		internal git_tag *tag;

		internal Tag(git_object *tag)
			: this((git_tag *)tag)
		{
		}

		internal Tag(git_tag *tag)
			: base((git_object *)tag)
		{
			this.tag = tag;
		}

		public Tag(Repository repository)
			: base(repository, git_otype.GIT_OBJ_TAG)
		{
			this.tag = (git_tag *)obj;
		}

		public GitObject Target
		{
			get {
				return GitObject.Create(NativeMethods.git_tag_target(tag));
			}
			set {
				NativeMethods.git_tag_set_target(tag, value.obj);
			}
		}

		public T GetTarget<T>() where T : GitObject
		{
			return GitObject.Create<T>(NativeMethods.git_tag_target(tag));
		}

		public string Name
		{
			get {
				return new string(NativeMethods.git_tag_name(tag));
			}
			set {
				NativeMethods.git_tag_set_name(tag, value);
			}
		}

		public Signature Tagger
		{
			get {
				return new Signature(NativeMethods.git_tag_tagger(tag));
			}
			set {
				NativeMethods.git_tag_set_tagger(tag, value.signature);
			}
		}

		public string Message
		{
			get {
				return new string(NativeMethods.git_tag_message(tag));
			}
			set {
				NativeMethods.git_tag_set_message(tag, value);
			}
		}
	}
}