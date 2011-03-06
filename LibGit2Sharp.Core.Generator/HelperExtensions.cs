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

namespace LibGit2Sharp.Core.Generator
{
	public static class HelperExtensions
	{
		public static string Join(this string[] stringArray, char c)
		{
			return stringArray.Join(new string(new char[] { c }));
		}

		public static string Join(this string[] stringArray, string delimeter)
		{
			return stringArray.Join(0, delimeter);
		}

		public static string Join(this string[] stringArray, int startIndex, char c)
		{
			return stringArray.Join(startIndex, new string(new char[] { c }));
		}

		public static string Join(this string[] stringArray, int startIndex, string delimeter)
		{
			if (stringArray.Length < startIndex)
				return string.Empty;
	
			System.Text.StringBuilder sb = new System.Text.StringBuilder(stringArray[startIndex]);

			for (int i = startIndex + 1; i < stringArray.Length; i++) {
				sb.Append(delimeter);
				sb.Append(stringArray[i]);
			}
	
			return sb.ToString();
		}
	}
}