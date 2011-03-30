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
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    unsafe public class DatabaseObject
    {
        internal git_odb_object *odb_object;
    
        internal DatabaseObject(git_odb_object *odb_object)
        {
            this.odb_object = odb_object;
        }
        
        public ObjectId ObjectId
        {
            get
            {
                return new ObjectId(NativeMethods.git_odb_object_id(odb_object));
            }
        }

        public uint Length
        {
            get {
                return NativeMethods.git_odb_object_size(odb_object);
            }
        }
    
        public git_otype Type
        {
            get {
                return NativeMethods.git_odb_object_type(odb_object);
            }
        }

        internal void *RawData
        {
            get {
                return NativeMethods.git_odb_object_data(odb_object);
            }
        }

        public IntPtr DataPointer
        {
            get {
                return new IntPtr(RawData);
            }
        }

        public byte[] GetData()
        {
            byte[] rawData = new byte[Length];
            Marshal.Copy(DataPointer, rawData, 0, (int)Length);
            return rawData;
        }

        public NativeMemoryStream GetNativeMemoryStream()
        {
            IntPtr data = DataPointer;
            if (data == IntPtr.Zero)
                return null;

            return new NativeMemoryStream(data, Length);
        }

        public void Close()
        {
            NativeMethods.git_odb_object_close(odb_object);
        }
    }
}
