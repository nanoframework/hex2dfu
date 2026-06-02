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
    /// Target PRefix of a DFU Image
    /// </summary>
    public class TargetPrefix
    {
        private string _targetName = string.Empty;

        /// <summary>
        /// Signature = Target
        /// </summary>
        public const string Signature = "Target";

        /// <summary>
        /// Alternate settings
        /// </summary>
        public bool AlternateSetting { get; set; }

        /// <summary>
        /// Is there a target name
        /// </summary>
        public bool IsTargetName => !string.IsNullOrEmpty(_targetName);

        /// <summary>
        /// The target name, max 254 characters
        /// </summary>
        public string TargetName
        {
            get => _targetName;
            set
            {
                if (value.Length > 254)
                {
                    throw new ArgumentException($"{nameof(TargetName)} can't be more than 254 characters.");
                }

                _targetName = value;
            }
        }

        /// <summary>
        /// The target size
        /// </summary>
        public uint TargetSize { get; set; }

        /// <summary>
        /// Tyhe number of elements
        /// </summary>
        public uint NumberOfElements { get; set; }

        /// <summary>
        /// Serialize theTarget Prefix
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            byte[] ser = new byte[274];
            Encoding.ASCII.GetBytes(Signature).CopyTo(ser, 0);
            ser[6] = AlternateSetting ? (byte)1 : (byte)0;
            ser[7] = IsTargetName ? (byte)1 : (byte)0;
            if (IsTargetName)
            {
                Encoding.ASCII.GetBytes(TargetName).CopyTo(ser, 11);
            }
            BinaryPrimitives.WriteUInt32LittleEndian(ser.AsSpan(266, 4), TargetSize);
            BinaryPrimitives.WriteUInt32LittleEndian(ser.AsSpan(270, 4), NumberOfElements);
            return ser;
        }
    }
}
