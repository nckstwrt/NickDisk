﻿//
// Copyright (c) 2014, Quamotion
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;

namespace DiscUtils.HfsPlus
{
    internal class CompressionAttribute
    {
        private uint _recordType;
        private uint _reserved1;
        //private uint _reserved2; // Nick
        private uint _attrSize;
        //private byte _attrData1; // Nick
        //private byte _attrData2; // Nick
        private uint _compressionMagic;
        private uint _compressionType;
        private uint _uncompressedSize;
        private uint _reserved3;

        public int ReadFrom(byte[] buffer, int offset)
        {
            _recordType = Utilities.ToUInt32BigEndian(buffer, offset + 0);
            _reserved1 = Utilities.ToUInt32BigEndian(buffer, offset + 4);
            _reserved1 = Utilities.ToUInt32BigEndian(buffer, offset + 8);
            _attrSize = Utilities.ToUInt32BigEndian(buffer, offset + 12);
            _compressionMagic = Utilities.ToUInt32BigEndian(buffer, offset + 16);
            _compressionType = Utilities.ToUInt32LittleEndian(buffer, offset + 20);
            _uncompressedSize = Utilities.ToUInt32LittleEndian(buffer, offset + 24);
            _reserved3 = Utilities.ToUInt32BigEndian(buffer, offset + 28);

            return Size;
        }

        public string CompressionMagic
        {
            get
            {
                return Encoding.ASCII.GetString(BitConverter.GetBytes(this._compressionMagic));
            }
        }

        public static int Size
        {
            get { return 32; }
        }

        public uint AttrSize
        {
            get { return _attrSize; }
        }

        public uint CompressionType
        {
            get { return _compressionType; } 
        }

        public uint UncompressedSize
        {
            get { return _uncompressedSize; }
        }
    }
}
