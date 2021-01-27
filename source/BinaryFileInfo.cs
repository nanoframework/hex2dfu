//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Tools
{
    /// <summary>
    /// Binary file info
    /// </summary>
    public class BinaryFileInfo
    {
        /// <summary>
        /// The file name
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The address
        /// </summary>
        public uint Address { get; private set; }

        /// <summary>
        /// Binary file info constructor
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="address">Address</param>
        public BinaryFileInfo(string fileName, uint address)
        {
            FileName = fileName;
            Address = address;
        }
    }
}
