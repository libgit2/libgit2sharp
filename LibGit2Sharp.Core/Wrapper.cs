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
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal enum git_rtype : int {
        GIT_REF_INVALID  = -1,
        GIT_REF_OID      =  1,
        GIT_REF_SYMBOLIC =  2,
        GIT_REF_PACKED   =  4,
        GIT_REF_HAS_PEEL =  8,
    };
    
    unsafe internal struct git_tag
    {
    }
    
    unsafe internal struct git_revwalk
    {
    }
    
    unsafe internal struct git_oid
    {
        public fixed byte oid[ObjectId.RawSize];
    }
    
    unsafe internal struct git_index_time
    {
        public int time;
        public int nanoseconds;
    }
    
    unsafe internal struct git_odb
    {
    }
    
    unsafe internal struct git_odb_backend
    {
    }
    
    unsafe internal struct git_rawobj
    {
        public IntPtr data;
        public uint len1;
        public git_otype type;
    }
    
    unsafe internal struct git_reference
    {
    }
    
    unsafe internal struct git_signature
    {
        public sbyte *name;
        public sbyte *email;
        public int time;
        public int offset;
    }
    
    unsafe internal struct git_index_entry
    {
        public git_index_time ctime;
    
        public git_index_time mtime;
    
        public int dev;
        public int ino;
        public int mode;
        public int uid;
        public int gid;
    
        public int file_size1;
        public int file_size2;
    
        public git_oid oid;
    
        public short flags;
        public short flags_extended;
    
        public sbyte *path;
    };
    
    
    public enum git_otype : int
    {
        GIT_OBJ_ANY       = -2,
        GIT_OBJ_BAD       = -1,
        GIT_OBJ__EXT1     =  0,
        GIT_OBJ_COMMIT    =  1,
        GIT_OBJ_TREE      =  2,
        GIT_OBJ_BLOB 	  =  3,
        GIT_OBJ_TAG 	  =  4,
        GIT_OBJ__EXT2     =  5,
        GIT_OBJ_OFS_DELTA =  6,
        GIT_OBJ_REF_DELTA =  7
    }
    
    unsafe internal struct git_vector
    {
        public uint _alloc_size;
        public void *_cmp;
        public void **contents;
        public uint length;
        public int sorted;
    };
    
    unsafe internal struct git_index
    {
        public git_repository *repository;
        public sbyte *index_file_path;
    
        public int time1;
    
        public git_vector entries;
    
        public int sorted;
    }
    
    unsafe internal struct git_object
    {
        public git_oid oid;
        public git_repository *repo;
        // TODO: implement rest of fields
        public int bla1;
        public int bla2;
        public int bla3;
        public int bla4;
    }
    
    unsafe internal struct git_blob
    {
        public git_object obj;
    }
    
    unsafe internal struct git_commit
    {
        public git_object obj;
        public git_vector parents;
        public git_tree *tree;
        public git_signature *author;
        public git_signature *committer;
        public sbyte *message;
        public sbyte *message_short;
        public int full_parse;
    }

    unsafe internal struct git_tree_entry
    {
        public uint attr;
        public sbyte *filename;
        public git_oid oid;
        public git_tree *owner;
    }

    unsafe internal struct git_tree
    {
        public git_object obj;
        // TODO: implement rest of the fields
    }

    unsafe internal struct git_repository
    {
        public git_odb *git_odb;
        public git_index *index;
    
        public void *objects;
        public git_vector memory_objects;
    
        public void *pointer1;
        public void *pointer2;
    
        public sbyte *path_repository;
        public sbyte *path_index;
        public sbyte *path_odb;
        public sbyte *path_workdir;
    
        public int is_bare;
    }
}
