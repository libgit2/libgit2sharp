using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public class RefsResolver : IRefsResolver
    {
        private readonly IntPtr _repositoryPtr = IntPtr.Zero;

        public RefsResolver(IntPtr repositoryPtr)
        {
            #region Parameters Validation

            if (repositoryPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException("repositoryPtr");
            }

            #endregion

            _repositoryPtr = repositoryPtr;
        }

        public Ref Resolve(string referenceName, bool shouldRecursivelyPeel)
        {
            IntPtr referencePtr;

            git_rtype retrieved;
            OperationResult result = NativeMethods.wrapped_git_reference_lookup(out referencePtr, out retrieved, _repositoryPtr, referenceName, shouldRecursivelyPeel);

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return BuildFrom(referencePtr, retrieved);

                case OperationResult.GIT_ENOTFOUND:
                    return null;

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }

        private static Ref BuildFrom(IntPtr referencePtr, git_rtype referenceType)
        {
            switch (referenceType)
            {
                case git_rtype.GIT_REF_OID:
                    var oidRef = (git_reference_oid)Marshal.PtrToStructure(referencePtr, typeof(git_reference_oid));
                    return new Ref(oidRef.@ref.name, ObjectId.ToString(oidRef.oid.id));

                default:
                    throw new ArgumentException(string.Format("Unexpected value {0}.", Enum.GetName(typeof(git_rtype), referenceType)), "referenceType");
            }
        }
    }
}