//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Tools
{
    /// <summary>
    /// The type of HEX field
    /// </summary>
    public enum HexFieldType
    {
        /// <summary>
        /// Data type
        /// </summary>
        Data = 0,

        /// <summary>
        /// End of File
        /// </summary>
        EndOfFile = 1,

        /// <summary>
        /// Extended Address
        /// </summary>
        ExtendedAddress = 2,

        /// <summary>
        /// Start Segment Address Record
        /// </summary>
        StartSegmentAddressRecord = 3,

        /// <summary>
        /// Extended Linear Address Record
        /// </summary>
        ExtendedLinearAddressRecord = 4,

        /// <summary>
        /// Start Linear Address Record
        /// </summary>
        StartLinearAddressRecord = 5,
    }
}
