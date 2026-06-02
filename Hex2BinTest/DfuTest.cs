//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using Xunit;
using nanoFramework.Tools;
using System.Buffers.Binary;

namespace Hex2BinTest
{
    public class DfuTest
    {
        [Fact]
        public void TestPrefix()
        {
            // Arrange
            DfuPrefix dfuPrefix = new DfuPrefix() { ImageSize = 2048, NumberOfTargets = 5 };
            // Act
            var ser = dfuPrefix.Serialize();
            // Assert
            Assert.Equal(11, ser.Length);
            // D
            Assert.Equal(0x44, ser[0]);
            // e
            Assert.Equal(0x65, ser[4]);
            Assert.Equal(0x01, ser[5]);
            var size = BinaryPrimitives.ReadUInt32LittleEndian(ser.AsSpan(6, 4));
            Assert.Equal((uint)2048, size);
            Assert.Equal(5, ser[10]);
        }

        [Fact]
        public void TestDfuSuffix()
        {
            // Arrange
            DfuSuffix dfuSuffix = new DfuSuffix() { FirmwareVersion = 0x1234, ProductId = 0x5678, VersionId = 0x90AB, Crc = 0xCDEF0123 };
            // Act
            var ser = dfuSuffix.Serialize();
            var firmware = BinaryPrimitives.ReadUInt16LittleEndian(ser.AsSpan(0, 2));
            var productId = BinaryPrimitives.ReadUInt16LittleEndian(ser.AsSpan(2, 2));
            var versionId = BinaryPrimitives.ReadUInt16LittleEndian(ser.AsSpan(4, 2));
            var ufd = BinaryPrimitives.ReadUInt16LittleEndian(ser.AsSpan(6, 2));
            // Assert
            Assert.Equal(dfuSuffix.FirmwareVersion, firmware);
            Assert.Equal(dfuSuffix.ProductId, productId);
            Assert.Equal(dfuSuffix.VersionId, versionId);
            Assert.Equal((ushort)0x011A, ufd);
            Assert.Equal(0x44, ser[10]);
            Assert.Equal(16, ser[11]);
            Assert.Equal(0x23, ser[12]);
            Assert.Equal(0xCD, ser[15]);
        }

        [Fact]
        public void TestTargetPRefix()
        {
            // Arrange
            TargetPrefix targetPrefix = new TargetPrefix() { AlternateSetting = false, NumberOfElements = 5, TargetName = "Ellerbach", TargetSize = 0x12345678 };
            // Act
            var ser = targetPrefix.Serialize();
            var size = BinaryPrimitives.ReadUInt32LittleEndian(ser.AsSpan(266, 4));
            var elemns = BinaryPrimitives.ReadUInt32LittleEndian(ser.AsSpan(270, 4));
            // Assert
            // T
            Assert.Equal(0x54, ser[0]);
            // t
            Assert.Equal(0x74, ser[5]);
            Assert.Equal(0x00, ser[6]);
            Assert.Equal(0x01, ser[7]);
            // E
            Assert.Equal(0x45, ser[11]);
            // h
            Assert.Equal(0x68, ser[11 + targetPrefix.TargetName.Length - 1]);
            Assert.Equal((uint)0x12345678, size);
            Assert.Equal((uint)5, elemns);
        }

        [Fact]
        public void TestImageElement()
        {
            // Arrange
            ImageElement imageElement = new ImageElement() { ElementAddress = 0x12345678, Data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF } };
            // Act
            var ser = imageElement.Serialize();
            var address = BinaryPrimitives.ReadUInt32LittleEndian(ser.AsSpan(0, 4));
            var size = BinaryPrimitives.ReadUInt32LittleEndian(ser.AsSpan(4, 4));
            // Assert
            Assert.Equal(imageElement.ElementAddress, address);
            Assert.Equal(imageElement.ElementSize, size);
            Assert.Equal((uint)imageElement.Data.Length, size);
            Assert.Equal(0x12, ser[8]);
            Assert.Equal(0xEF, ser[15]);
        }

        [Fact]
        public void TestDfuImage()
        {
            // Arrange
            DfuImage dfuImage = new DfuImage();
            dfuImage.TargetPrefix.AlternateSetting = false;
            dfuImage.TargetPrefix.TargetName = "Ellerbach" ;
            for (int i = 0; i < 5; i++)
            {
                var imageElement = new ImageElement() { ElementAddress = (uint)(16 * i), Data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF } };
                dfuImage.ImageElements.Add(imageElement);
            }
            dfuImage.TargetPrefix.NumberOfElements = (uint)dfuImage.ImageElements.Count;
            // Act
            var ser = dfuImage.Serialize();
            // Assert
            var size = BinaryPrimitives.ReadUInt32LittleEndian(ser.AsSpan(266, 4));
            var elemns = BinaryPrimitives.ReadUInt32LittleEndian(ser.AsSpan(270, 4));
            // Assert
            // T
            Assert.Equal(0x54, ser[0]);
            // t
            Assert.Equal(0x74, ser[5]);
            Assert.Equal(0x00, ser[6]);
            Assert.Equal(0x01, ser[7]);
            // E
            Assert.Equal(0x45, ser[11]);
            // h
            Assert.Equal(0x68, ser[11 + dfuImage.TargetPrefix.TargetName.Length - 1]);
            Assert.Equal(dfuImage.TargetPrefix.TargetSize, size);
            Assert.Equal(dfuImage.TargetPrefix.NumberOfElements, elemns);
            Assert.Equal(0x00, ser[274]);
        }
    }
}
