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
using System.Collections.Generic;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public class ReferenceManager : IReferenceManager
    {
        private readonly Core.Repository _coreRepository;

        public ReferenceManager(Core.Repository coreRepository)
        {
            #region Parameters Validation

            if (coreRepository == null)
            {
                throw new ArgumentNullException("coreRepository");
            }

            #endregion

            _coreRepository = coreRepository;
        }

        public IList<Ref> RetrieveAll()
        {
            throw new NotImplementedException();
        }

        public Ref Head
        {
            get { return Lookup(Constants.GIT_HEAD_FILE, false); }
        }

        public Ref Lookup(string referenceName, bool shouldEagerlyPeel)
        {
            Core.Reference reference = _coreRepository.ReferenceLookup(referenceName);

            if (!shouldEagerlyPeel)
            {
                return BuildFrom(reference, referenceName);
            }

            Core.ObjectIdReference oidReference = reference.Resolve();
            return BuildFrom(oidReference, referenceName);

        }

        private static Ref BuildFrom(Core.Reference reference, string referenceName)
        {
            switch (reference.Type)
            {
                case Core.git_rtype.GIT_REF_OID:
                    return new Ref(referenceName, ((Core.ObjectIdReference)reference).ObjectId.ToString());

                case Core.git_rtype.GIT_REF_SYMBOLIC:
                    return new Ref(referenceName, ((Core.SymbolicReference)reference).Target);

                default:
                    throw new Exception(string.Format("Unexpected reference type ({0}).", Enum.GetName(typeof(Core.git_rtype), reference.Type)));
            }
        }
    }
}
