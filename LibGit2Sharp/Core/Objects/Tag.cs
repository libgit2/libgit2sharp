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
        
        public static ObjectId Create(Repository repository,
                                      string tagName,
                                      ObjectId target,
                                      git_otype type,
                                      Signature tagger,
                                      string message)
        {
            ObjectId oid = new ObjectId();
            int ret = NativeMethods.git_tag_create(&oid.oid,
                                                  repository.repository,
                                                  tagName,
                                                  &target.oid,
                                                  type,
                                                  tagger.signature,
                                                  message);

            GitError.Check(ret);

            return oid;
        }
        
        public GitObject Target
        {
            get {
                git_object *obj;
                int ret = NativeMethods.git_tag_target(&obj, tag);
                GitError.Check(ret);
                return GitObject.Create(obj);
            }
        }

        public ObjectId TargetOid
        {
            get {
                return new ObjectId(NativeMethods.git_tag_target_oid(tag));
            }
        }
        
        public T GetTarget<T>() where T : GitObject
        {
            git_object *obj;
            int ret = NativeMethods.git_tag_target(&obj, tag);
            GitError.Check(ret);
            return GitObject.Create<T>(obj);
        }
        
        public string Name
        {
            get {
                return new string(NativeMethods.git_tag_name(tag));
            }
        }
    
        public Signature Tagger
        {
            get {
                return new Signature(NativeMethods.git_tag_tagger(tag));
            }
        }
    
        public string Message
        {
            get {
                return new string(NativeMethods.git_tag_message(tag));
            }
        }
    }
}
