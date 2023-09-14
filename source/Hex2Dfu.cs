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
            using StreamReader reader = new StreamReader(hexFile);
            using MemoryStream binDestination = new MemoryStream();
            string hexLine = string.Empty;
            long offset = 0;
            long maxSize = 0;
            Console.WriteLine($"Converting HEX file {hexFile} to BIN format.");
            while (reader.Peek() >= 0)
            {
                hexLine = reader.ReadLine();
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
                    binDestination.Seek(offset + hex.Address, SeekOrigin.Begin);
                    maxSize = maxSize < offset + hex.Address ? offset + hex.Address : maxSize;
                    binDestination.Write(hex.Data);
                }
                else if (hex.HexFieldType == HexFieldType.ExtendedAddress)
                {
                    offset = BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 4;
                }
                    else if (hex.HexFieldType == HexFieldType.ExtendedLinearAddressRecord)
                    {
                        offset = BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 16;
                    }
                else if (hex.HexFieldType == HexFieldType.EndOfFile)
                {
                    break;
                }
            }

            // Close the reader
            reader.Close();
            reader.Dispose();
            // Save the file result and close the memory stream and the file
            using FileStream fs = new FileStream(hexFile + ".bin", FileMode.Create);
            Span<byte> binFile = binDestination.GetBuffer().AsSpan(0, (int)maxSize);
            binDestination.Close();
            binDestination.Dispose();
            fs.Write(binFile);
            fs.Close();
            fs.Dispose();

            DfuFile dfuFile = new();
            DfuImage dfuImage = new();
            dfuImage.TargetPrefix.TargetName = GetCleanName(dfuName);
            ImageElement imageElement = new();
            imageElement.Data = binFile.ToArray();
            dfuImage.ImageElements.Add(imageElement);
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
                using FileStream fs = new(binFile.FileName, FileMode.Open);
                ImageElement imageElement = new();
                imageElement.Data = new byte[fs.Length];
                imageElement.ElementAddress = binFile.Address;
                if (fs.Read(imageElement.Data) <= 0)
                {
                    Console.WriteLine();
                    Console.WriteLine($"ERROR: adding {binFile.FileName}");
                    Console.WriteLine();
                    return false;
                }

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
