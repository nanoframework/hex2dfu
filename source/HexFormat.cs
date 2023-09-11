//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using System.Linq;

namespace nanoFramework.Tools
{
    /// <summary>
    /// An HEX format line
    /// </summary>
    public class HexFormat
    {
        /// <summary>
        /// Get the number of bytes available
        /// </summary>
        public byte NumberOfBytes { get; internal set; }

        /// <summary>
        /// The address
        /// </summary>
        public ushort Address { get; internal set; }

        public HexFieldType HexFieldType { get; internal set; }

        /// <summary>
        /// The actual data
        /// </summary>
        public byte[] Data { get; internal set; }

        /// <summary>
        /// The CRC byte
        /// </summary>
        public byte Crc { get; internal set; }

        /// <summary>
        /// Is it a valid record? Yes if line starts with : and the CRC is valid
        /// </summary>
        public bool IsValidRecord { get; internal set; }

        /// <summary>
        /// A Hex format line
        /// </summary>
        /// <param name="hexLine">The string to convert</param>
        public HexFormat(string hexLine)
        {
            var data = ConvertHex2Bin(hexLine);
            IsValidRecord = (data != null) && (data.Length >= 5);
            if (!IsValidRecord)
            {
                return;
            }

            Crc = data[data.Length - 1];
            IsValidRecord = CheckCrc(data);

            NumberOfBytes = data[0];
            Address = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan().Slice(1, 2));
            HexFieldType = (HexFieldType)data[3];
            Data = new byte[NumberOfBytes];            
            // no data bytes in the end of file line.
            if (NumberOfBytes > 0)
            {
                data.AsSpan(4, NumberOfBytes).CopyTo(Data);
            }
        }
        
        private byte[] ConvertHex2Bin(string input)
        {
            if (input.Length < 1)
            {
                return null;
            }

            if (input.ToCharArray()[0] != ':')
            {
                return null;
            }

            var subInput = input.Substring(1);
            return Enumerable.Range(0, subInput.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(subInput.Substring(x, 2), 16))
                .ToArray();
        }

        private bool CheckCrc(byte[] data)
        {
            int crc = 0;
            // crc is calculated for all data but the last byte (checksum)
            for (int i = 0; i < data.Length - 1; i++) 
            {
                crc += data[i];
            }
            // Two's complement on checksum
            return (byte)(~crc + 1) == data[data.Length - 1];
        }
    }
}
