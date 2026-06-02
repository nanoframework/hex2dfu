//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using System.Text;

namespace nanoFramework.Tools
{
    /// <summary>
    /// An image element of a DFU Image
    /// </summary>
    public class ImageElement
    {
        /// <summary>
        /// The Element Address
        /// </summary>
        public uint ElementAddress { get; set; }

        /// <summary>
        /// The size of the Data
        /// </summary>
        public uint ElementSize => Data != null ? (uint)Data.Length : 0;

        /// <summary>
        /// The Data
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Serialize the Image Element
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            byte[] ser = new byte[8 + ElementSize];
            BinaryPrimitives.WriteUInt32LittleEndian(ser.AsSpan(0, 4), ElementAddress);
            BinaryPrimitives.WriteUInt32LittleEndian(ser.AsSpan(4, 4), ElementSize);
            if (ElementSize > 0)
            {
                Data.CopyTo(ser, 8);
            }
            return ser;
        }
    }
}
