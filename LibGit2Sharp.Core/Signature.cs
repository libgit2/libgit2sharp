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
    // TODO: implement the time fields
    unsafe public class Signature : IDisposable
    {
        internal git_signature *signature = null;
    
        internal Signature(git_signature *signature)
        {
            this.signature = signature;
        }
    
        public Signature(string name, string email)
            : this(NativeMethods.git_signature_new(name, email, 0, 0))
        {
        }

        // indicates if it is managed by a repository or not
        internal bool Managed { get; set; }

        public string Name
        {
            get {
                return new string(signature->name);
            }
        }
        
        public string Email
        {
            get {
                return new string(signature->email);
            }
        }
    
        public int Time
        {
            get {
                return signature->time;
            }
        }
    
        public int Offset
        {
            get {
                return signature->offset;
            }
        }
    
        public Signature Clone()
        {
            return new Signature(NativeMethods.git_signature_dup(signature));
        }

        public void Free()
        {
            NativeMethods.git_signature_free(signature);
            signature = null;
        }
    
        #region IDisposable implementation
        public void Dispose()
        {
            if (!Managed)
                Free();
        }
        #endregion
    }
}
