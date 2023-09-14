//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

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
            uint startAddress = 0;
            long offset = 0;
            long maxSize = 0;
            List<ImageElement> imageElements = new List<ImageElement>();
            ImageElement currentImageElement = new ImageElement();
            bool elementAddressSet = false;

            using (StreamReader reader = new StreamReader(hexFile))
            using (MemoryStream binDestination = new MemoryStream())
            {
                Console.WriteLine($"Converting HEX file {hexFile} to BIN format.");

                string hexLine;
                while ((hexLine = reader.ReadLine()) != null)
                {
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
                        if (offset + hex.Address - currentImageElement.ElementAddress > binDestination.Length)
                        {
                            currentImageElement.Data = binDestination.ToArray();
                            imageElements.Add(currentImageElement);
                            currentImageElement = new ImageElement();
                            currentImageElement.ElementAddress = (uint)offset + hex.Address;
                            binDestination.SetLength(0);
                        }

                        elementAddressSet = true;
                        binDestination.Seek(offset + hex.Address - currentImageElement.ElementAddress, SeekOrigin.Begin);
                        maxSize = Math.Max(maxSize, offset + hex.Address - currentImageElement.ElementAddress);
                        binDestination.Write(hex.Data);
                    }
                    else if (hex.HexFieldType == HexFieldType.ExtendedAddress)
                    {
                        if (!elementAddressSet)
                        {
                            startAddress = (uint)BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 4;
                        }

                        offset = BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 4;
                    }
                    else if (hex.HexFieldType == HexFieldType.ExtendedLinearAddressRecord)
                    {
                        if (!elementAddressSet)
                        {
                            currentImageElement.ElementAddress = (uint)BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 16;
                            elementAddressSet = true;
                        }

                        offset = BinaryPrimitives.ReadUInt16BigEndian(hex.Data) << 16;
                    }
                    else if (hex.HexFieldType == HexFieldType.EndOfFile)
                    {
                        break;
                    }
                }

                currentImageElement.Data = binDestination.ToArray();
                imageElements.Add(currentImageElement);
            }

            string baseFileName = Path.GetFileNameWithoutExtension(hexFile);

            for (int i = 0; i < imageElements.Count; i++)
            {
                ImageElement imageElement = imageElements[i];
                string binFileName = $"{baseFileName}_element_{i}.bin";
                if(imageElements.Count > 1)
                    binFileName += $"_element_{i}";
                binFileName += ".bin";

                using (FileStream fs = new FileStream(binFileName, FileMode.Create))
                {
                    fs.Write(imageElement.Data);
                }

                Console.WriteLine($"Binary file generated: {binFileName}");
            }

            DfuFile dfuFile = new DfuFile();
            DfuImage dfuImage = new DfuImage();
            dfuImage.TargetPrefix.TargetName = GetCleanName(dfuName);
            dfuImage.ImageElements.AddRange(imageElements);
            dfuFile.DfuImages.Add(dfuImage);
            dfuFile.DfuSuffix.FirmwareVersion = fwVersion;
            dfuFile.DfuSuffix.ProductId = pid;
            dfuFile.DfuSuffix.VersionId = vid;
            var ser = dfuFile.Serialize();

            using (FileStream fdest = new FileStream(dfuName, FileMode.Create))
            {
                fdest.Write(ser);
            }

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
