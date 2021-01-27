//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace nanoFramework.Tools
{
    /// <summary>
    /// The DFU image
    /// </summary>
    public class DfuImage
    {
        /// <summary>
        /// The target prefix
        /// </summary>
        public TargetPrefix TargetPrefix { get; } = new TargetPrefix();

        /// <summary>
        /// The Image Elements contained into the DFU Image
        /// </summary>
        public List<ImageElement> ImageElements { get; } = new List<ImageElement>();

        /// <summary>
        /// Serialize the DFU Image
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            List<byte> ser = new List<byte>();
            List<byte> elements = new List<byte>();
            
            foreach(var img in ImageElements)
            {
                elements.AddRange(img.Serialize());
            }

            TargetPrefix.NumberOfElements = (uint)ImageElements.Count;
            TargetPrefix.TargetSize = (uint)(elements.Count);
            ser.AddRange(TargetPrefix.Serialize());
            ser.AddRange(elements);
            return ser.ToArray();
        }
    }
}
