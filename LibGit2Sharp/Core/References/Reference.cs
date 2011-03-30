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
using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
    unsafe abstract public class Reference
    {
        internal git_reference *reference = null;
    
        internal Reference()
        {
        }
    
        public string Name
        {
            get { return new string(NativeMethods.git_reference_name(reference)); }
        }

        public ObjectIdReference Resolve()
        {
            git_reference *resolved_ref = null;
    
            int ret = NativeMethods.git_reference_resolve(&resolved_ref, reference);
            GitError.Check(ret);
    
            return Reference.Create(resolved_ref) as ObjectIdReference;
        }

        public Repository Owner
        {
            get {
                return new Repository(NativeMethods.git_reference_owner(reference));
            }
        }
        
        public void Rename(string newName)
        {
            int ret = NativeMethods.git_reference_rename(reference, newName);
            GitError.Check(ret);
        }
        
        public void Delete()
        {
            int ret = NativeMethods.git_reference_delete(reference);
            GitError.Check(ret);
        }

        public git_rtype Type
        {
            get {
                return NativeMethods.git_reference_type(reference);
            }
        }

        public static Type GetClass(git_rtype type)
        {
            switch (type)
            {
            case git_rtype.GIT_REF_SYMBOLIC:
                return typeof(SymbolicReference);
            case git_rtype.GIT_REF_OID:
                return typeof(ObjectIdReference);
            default:
                return null;
            }
        }

        public static git_rtype GetType(Type type)
        {
            if (type == typeof(ObjectIdReference)) {
                return git_rtype.GIT_REF_OID | git_rtype.GIT_REF_PACKED;
            } else if (type == typeof(SymbolicReference)) {
                return git_rtype.GIT_REF_SYMBOLIC | git_rtype.GIT_REF_PACKED;
            } else if (type == typeof(Reference)) {
                return git_rtype.GIT_REF_SYMBOLIC | git_rtype.GIT_REF_OID | git_rtype.GIT_REF_PACKED;
            } else {
                return (git_rtype)git_rtype.GIT_REF_INVALID;
            }
        }

        public static List<T> ListAll<T>(Repository repo) where T : Reference
        {
            List<T> list = new List<T>();
            foreach (string refname in ListAllStrings<T>(repo)) {
                list.Add((T)repo.ReferenceLookup(refname));
            }
            return list;
        }

        public static List<Reference> ListAll(Repository repo)
        {
            return ListAll<Reference>(repo);
        }

        public static List<string> ListAllStrings(Repository repo)
        {
            return ListAllStrings(repo, (uint)(GetType(typeof(Reference))));
        }

        public static List<string> ListAllStrings<T>(Repository repo)
        {
            return ListAllStrings(repo, (uint)GetType(typeof(T)));
        }

        public static List<string> ListAllStrings(Repository repo, uint filter)
        {
            List<string> list = new List<string>();
            git_strarray strarr;
            int ret = NativeMethods.git_reference_listall(&strarr, repo.repository, filter);
            GitError.Check(ret);
            for (uint i = 0; i < strarr.size; i++) {
                list.Add(new string(strarr.strings[i]));
            }
            NativeMethods.git_strarray_free(&strarr);
            return list;
        }

        internal static Reference Create(git_reference *reference)
        {
            switch (NativeMethods.git_reference_type(reference))
            {
                case git_rtype.GIT_REF_SYMBOLIC:
                    return new SymbolicReference(reference);
                case git_rtype.GIT_REF_OID:
                    return new ObjectIdReference(reference);
                case git_rtype.GIT_REF_INVALID:
                    throw new InvalidReferenceException();
                default:
                    throw new Exception("Reference type not yet implemented");
            }
        }
    }
}
