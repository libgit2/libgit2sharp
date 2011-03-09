/*
 * The MIT License
 *
 * Copyright (c) 2011 Andrius Bentkus
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
