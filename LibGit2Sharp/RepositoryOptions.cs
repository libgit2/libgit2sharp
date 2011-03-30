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
    /// <summary>
    ///   Optional parameters that can be defined when opening or creating a <see cref="Repository"/>
    /// </summary>
    public class RepositoryOptions
    {
        /// <summary>
        ///   Gets or sets a value indicating whether to create a new git repository if one does not exist at the specified path.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a repository should be created (if needed); otherwise, <c>false</c>.
        /// </value>
        public bool CreateIfNeeded { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether this is a bare git repository or whether a bare repository should be created.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this is a bare repository; otherwise, <c>false</c>.
        /// </value>
        public bool IsBareRepository { get; set; }
    }
}