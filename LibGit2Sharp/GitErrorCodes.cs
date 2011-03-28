#region  Copyright (c) 2011 LibGit2Sharp committers

//  The MIT License
//  
//  Copyright (c) 2011 LibGit2Sharp committers
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.

#endregion

namespace LibGit2Sharp
{
    public enum GitErrorCodes
    {
        /** Operation completed successfully. */
        GIT_SUCCESS = 0,

        /**
         * Operation failed, with unspecified reason.
         * This value also serves as the base error code; all other
         * error codes are subtracted from it such that all errors
         * are < 0, in typical POSIX C tradition.
         */
        GIT_ERROR = -1,

        /** Input was not a properly formatted Git object id. */
        GIT_ENOTOID = (GIT_ERROR - 1),

        /** Input does not exist in the scope searched. */
        GIT_ENOTFOUND = (GIT_ERROR - 2),

        /** Not enough space available. */
        GIT_ENOMEM = (GIT_ERROR - 3),

        /** Consult the OS error information. */
        GIT_EOSERR = (GIT_ERROR - 4),

        /** The specified object is of invalid type */
        GIT_EOBJTYPE = (GIT_ERROR - 5),

        /** The specified object has its data corrupted */
        GIT_EOBJCORRUPTED = (GIT_ERROR - 6),

        /** The specified repository is invalid */
        GIT_ENOTAREPO = (GIT_ERROR - 7),

        /** The object type is invalid or doesn't match */
        GIT_EINVALIDTYPE = (GIT_ERROR - 8),

        /** The object cannot be written that because it's missing internal data */
        GIT_EMISSINGOBJDATA = (GIT_ERROR - 9),

        /** The packfile for the ODB is corrupted */
        GIT_EPACKCORRUPTED = (GIT_ERROR - 10),

        /** Failed to adquire or release a file lock */
        GIT_EFLOCKFAIL = (GIT_ERROR - 11),

        /** The Z library failed to inflate/deflate an object's data */
        GIT_EZLIB = (GIT_ERROR - 12),

        /** The queried object is currently busy */
        GIT_EBUSY = (GIT_ERROR - 13),

        /** The index file is not backed up by an existing repository */
        GIT_EBAREINDEX = (GIT_ERROR - 14),

        /** The name of the reference is not valid */
        GIT_EINVALIDREFNAME = (GIT_ERROR - 15),

        /** The specified reference has its data corrupted */
        GIT_EREFCORRUPTED = (GIT_ERROR - 16),

        /** The specified symbolic reference is too deeply nested */
        GIT_ETOONESTEDSYMREF = (GIT_ERROR - 17),

        /** The pack-refs file is either corrupted of its format is not currently supported */
        GIT_EPACKEDREFSCORRUPTED = (GIT_ERROR - 18),

        /** The path is invalid */
        GIT_EINVALIDPATH = (GIT_ERROR - 19),

        /** The revision walker is empty; there are no more commits left to iterate */
        GIT_EREVWALKOVER = (GIT_ERROR - 20),

        /** The state of the reference is not valid */
        GIT_EINVALIDREFSTATE = (GIT_ERROR - 21),
    }
}