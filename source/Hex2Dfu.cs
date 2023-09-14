//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace nanoFramework.Tools
{
    /// <summary>
    /// HEX to DFU and BIN to DFU converter class
    /// </summary>
    public class Hex2Dfu
    {
        private const ushort DefaultSTMVid = 0x0483;
        private const ushort DefafultSTMPid = 0xDF11;
        private const ushort DefaultFwVersion = 0x2200;

        /// <summary>
        /// Concert a HEX file into a DFU file, save the intermediate BIN file
        /// </summary>
        /// <param name="hexFile">The original HEX file name</param>
        /// <param name="dfuName">The target DFU file name</param>
        /// <param name="vid">The Vendor ID</param>
        /// <param name="pid">The Product ID</param>
        /// <param name="fwVersion">The Firmware Version</param>
        /// <returns>True if success</returns>
        public static bool CreateDfuFile(string hexFile, string dfuName, ushort vid = DefaultSTMVid, ushort pid = DefafultSTMPid, ushort fwVersion = DefaultFwVersion)
        {
            // prepare target dfu file
            DfuFile dfuFile = new();
            DfuImage dfuImage = new();
            dfuImage.TargetPrefix.TargetName = GetCleanName(dfuName);
            ImageElement imageElement = new();

            using StreamReader reader = new StreamReader(hexFile);

            Console.WriteLine($"Converting HEX file {hexFile} to BIN format.");

            // store last encountered extended address here to detect gaps in the memory mapping (multiple segments)
            // Treat extended linear address the same way as extended segment address, assume both are not used in the same file.
            uint lastAddressOffset = 0;

            while (reader.Peek() >= 0)
            {
                string hexLine = reader.ReadLine();
                HexFormat hex = new HexFormat(hexLine);
                if (!hex.IsValidRecord)
                {
                    Console.WriteLine();
                    Console.WriteLine($"ERROR: Invalid HEX record");
                    Console.WriteLine();
                    throw new Exception($"Error reading hex file, invalid file or CRC error");
                }

                if (hex.HexFieldType == HexFieldType.Data)
                {
                    if (imageElement.Data.Count == 0)
                    {
                        // there may have been previous extended address record, add that base address
                        imageElement.ElementAddress = lastAddressOffset;
                        // first data record address is the lower 2 bytes of the memory start address.
                        imageElement.ElementAddress += hex.Address;
                    }

                    // gap in the memory space map, create a new image element and add old one to the collection.
                    // The gap can be caused by either extended address record or data address jump.
                    if (hex.Address + lastAddressOffset > imageElement.ElementAddress + imageElement.Data.Count)
                    {
                        dfuImage.ImageElements.Add(imageElement);
                        // create new element
                        imageElement = new ImageElement
                        {
                            ElementAddress = lastAddressOffset + hex.Address
                        };
                    }

                    imageElement.Data.AddRange(hex.Data);
                }
                else if (hex.HexFieldType == HexFieldType.ExtendedSegmentAddress)
                {
                    // ExtendedSegmentAddress record data if any is multiplied by 16 and added to the memory start address.
                    lastAddressOffset = (uint)BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 4;
                }
                else if (hex.HexFieldType == HexFieldType.ExtendedLinearAddress)
                {
                    // these are 2 upper bytes of the 4-byte extended address
                    lastAddressOffset = (uint)BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 16;
                }
                else if (hex.HexFieldType == HexFieldType.EndOfFile)
                {
                    if (imageElement.Data.Count > 0)
                    {
                        // add last element if any
                        dfuImage.ImageElements.Add(imageElement);
                    }
                    break;
                }
            }

            // Close the reader
            reader.Close();
            reader.Dispose();

            dfuFile.DfuImages.Add(dfuImage);
            dfuFile.DfuSuffix.FirmwareVersion = fwVersion;
            dfuFile.DfuSuffix.ProductId = pid;
            dfuFile.DfuSuffix.VersionId = vid;
            var ser = dfuFile.Serialize();
            using FileStream fdest = new FileStream(dfuName, FileMode.Create);
            fdest.Write(ser);
            fdest.Close();
            fdest.Dispose();
            Console.WriteLine();
            Console.WriteLine($"DFU generated: {dfuName}");
            Console.WriteLine($"Vendor ID: {vid.ToString("X4")}");
            Console.WriteLine($"Product ID: {pid.ToString("X4")}");
            Console.WriteLine($"Version: {fwVersion.ToString("X4")}");
            Console.WriteLine();
            return true;
        }

        /// <summary>
        /// Convert multiple BIN files with specific target address to a DFU file
        /// </summary>
        /// <param name="binFiles">A list of Binary File Information</param>
        /// <param name="dfuName">The target DFU file name</param>
        /// <param name="vid">The Vendor ID</param>
        /// <param name="pid">The Product ID</param>
        /// <param name="fwVersion">The Firmware Version</param>
        /// <returns></returns>
        public static bool CreateDfuFile(List<BinaryFileInfo> binFiles, string dfuName, ushort vid = DefaultSTMVid, ushort pid = DefafultSTMPid, ushort fwVersion = DefaultFwVersion)
        {
            DfuFile dfuFile = new();
            DfuImage dfuImage = new();
            dfuImage.TargetPrefix.TargetName = "nanoFramework";
            foreach (var binFile in binFiles)
            {
                ImageElement imageElement = new();
                imageElement.Data.AddRange(File.ReadAllBytes(binFile.FileName));
                imageElement.ElementAddress = binFile.Address;

                Console.WriteLine($"Adding file to image: {binFile.FileName}");
                dfuImage.ImageElements.Add(imageElement);
            }

            dfuFile.DfuImages.Add(dfuImage);
            dfuFile.DfuSuffix.FirmwareVersion = fwVersion;
            dfuFile.DfuSuffix.ProductId = pid;
            dfuFile.DfuSuffix.VersionId = vid;
            var ser = dfuFile.Serialize();
            using FileStream fdest = new FileStream(dfuName, FileMode.Create);
            fdest.Write(ser);
            fdest.Close();
            fdest.Dispose();
            Console.WriteLine();
            Console.WriteLine($"DFU generated: {dfuName}");
            Console.WriteLine($"Vendor ID: {vid.ToString("X4")}");
            Console.WriteLine($"Product ID: {pid.ToString("X4")}");
            Console.WriteLine($"Version: {fwVersion.ToString("X4")}");
            Console.WriteLine();
            return true;
        }

        private static string GetCleanName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName);
        }
    }
}
