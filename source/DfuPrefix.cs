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
    /// The DFU Prefix
    /// </summary>
    public class DfuPrefix
    {
        /// <summary>
        /// Signature = DfuSe
        /// </summary>
        public const string Signature = "DfuSe";

        /// <summary>
        /// The version = 0x01
        /// </summary>
        public const byte Version = 0x01;

        /// <summary>
        /// The image size
        /// </summary>
        public uint ImageSize { get; set; }

        /// <summary>
        /// The number of targets present in the image
        /// </summary>
        public byte NumberOfTargets { get; set; }

        /// <summary>
        /// Serialize this class
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            byte[] ser = new byte[11];
            Encoding.ASCII.GetBytes(Signature).CopyTo(ser, 0);
            ser[5] = Version;
            BinaryPrimitives.WriteUInt32LittleEndian(ser.AsSpan(6, 4), ImageSize);
            ser[10] = NumberOfTargets;
            return ser;
        }

    }
}
