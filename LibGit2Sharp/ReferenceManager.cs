/*
 * The MIT License
 *
 * Copyright (c) 2011 LibGit2Sharp committers
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
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class ReferenceManager
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
            List<Reference> coreRefs = Core.Reference.ListAll(_coreRepository);
            return coreRefs.ConvertAll(BuildFrom);
        }

        public Ref Head
        {
            get { return Lookup(Constants.GIT_HEAD_FILE, true); }
        }

        public Ref Lookup(string referenceName, bool shouldEagerlyPeel)
        {
            Core.Reference reference = _coreRepository.ReferenceLookup(referenceName);

            if (!shouldEagerlyPeel)
            {
                return BuildFrom(reference);
            }

            Core.ObjectIdReference oidReference = reference.Resolve();
            return BuildFrom(oidReference);

        }

        private static Ref BuildFrom(Core.Reference reference)
        {
            switch (reference.Type)
            {
                case Core.git_rtype.GIT_REF_OID:
                    return new Ref(reference.Name, ((Core.ObjectIdReference)reference).ObjectId.ToString());

                case Core.git_rtype.GIT_REF_SYMBOLIC:
                    return new Ref(reference.Name, ((Core.SymbolicReference)reference).Target);

                default:
                    throw new Exception(string.Format("Unexpected reference type ({0}).", Enum.GetName(typeof(Core.git_rtype), reference.Type)));
            }
        }
    }
}
