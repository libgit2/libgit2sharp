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
