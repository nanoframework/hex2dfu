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
    /// Dfu Suffix
    /// </summary>
    public class DfuSuffix
    {
        /// <summary>
        /// The firmware version
        /// </summary>
        public ushort FirmwareVersion { get; set; }

        /// <summary>
        /// The product ID
        /// </summary>
        public ushort ProductId { get; set; }

        /// <summary>
        /// The version ID
        /// </summary>
        public ushort VersionId { get; set; }

        /// <summary>
        /// The version of DFU file format = 0x011A
        /// </summary>
        public const ushort Version = 0x011A;

        /// <summary>
        /// The signature = UFD
        /// </summary>
        public const string Signature = "UFD";

        /// <summary>
        /// The CRC calculated over the whole file except the CRC data itself
        /// </summary>
        public uint Crc { get; set; }

        public byte[] Serialize()
        {
            byte[] ser = new byte[16];
            BinaryPrimitives.WriteUInt16LittleEndian(ser.AsSpan(0, 2), FirmwareVersion);
            BinaryPrimitives.WriteUInt16LittleEndian(ser.AsSpan(2, 2), ProductId);
            BinaryPrimitives.WriteUInt16LittleEndian(ser.AsSpan(4, 2), VersionId);
            BinaryPrimitives.WriteUInt16LittleEndian(ser.AsSpan(6, 2), Version);
            Encoding.ASCII.GetBytes(Signature).CopyTo(ser, 8);
            ser[11] = 16;
            BinaryPrimitives.WriteUInt32LittleEndian(ser.AsSpan(12, 4), Crc);
            return ser;
        }
    }
}
