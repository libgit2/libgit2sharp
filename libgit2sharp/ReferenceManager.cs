using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public class ReferenceManager : IReferenceManager
    {
        private readonly IntPtr _repositoryPtr = IntPtr.Zero;

        public ReferenceManager(IntPtr repositoryPtr)
        {
            #region Parameters Validation

            if (repositoryPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException("repositoryPtr");
            }

            #endregion

            _repositoryPtr = repositoryPtr;
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
            IntPtr referencePtr;

            git_rtype retrieved;
            OperationResult result = NativeMethods.wrapped_git_reference_lookup(out referencePtr, out retrieved, _repositoryPtr, referenceName, shouldEagerlyPeel);

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return BuildFrom(referencePtr, referenceName, retrieved);

                case OperationResult.GIT_ENOTFOUND:
                    return null;

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }

        private static Ref BuildFrom(IntPtr referencePtr, string referenceName, git_rtype referenceType)
        {
            switch (referenceType)
            {
                case git_rtype.GIT_REF_OID:
                    var oidRef = (git_reference_oid)Marshal.PtrToStructure(referencePtr, typeof(git_reference_oid));
                    return new Ref(referenceName, ObjectId.ToString(oidRef.oid.id));

                case git_rtype.GIT_REF_SYMBOLIC:
                    var symRef = (git_reference_symbolic)Marshal.PtrToStructure(referencePtr, typeof(git_reference_symbolic));
                    return new Ref(referenceName, symRef.target);

                default:
                    throw new ArgumentException(string.Format("Unexpected value {0}.", Enum.GetName(typeof(git_rtype), referenceType)), "referenceType");
            }
        }
    }
}