//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using System.IO;
using Xunit;
using nanoFramework.Tools;

namespace Hex2BinTest
{
    public class Hex2DfuTest : IDisposable
    {
        private readonly string _tempDir;

        public Hex2DfuTest()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"Hex2DfuTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private string TempHex(string name, params string[] lines)
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllLines(path, lines);
            return path;
        }

        // DFU layout: DfuPrefix(11) + TargetPrefix(274) + ImageElements... + DfuSuffix(16)
        private const int ElementsOffset = 11 + 274;

        private static uint ReadU32LE(byte[] data, int offset)
            => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));

        [Fact]
        public void CreateDfuFile_SingleElaRegion_ProducesOneImageElement()
        {
            // Arrange – one ELA record followed by 16 contiguous data bytes
            string hexFile = TempHex("single.hex",
                ":020000040800F2",                              // ELA → base 0x08000000
                ":1000000000000000000000000000000000000000F0",  // 16 zero bytes at offset 0
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "single.dfu");

            // Act
            bool result = Hex2Dfu.CreateDfuFile(hexFile, dfuFile);

            // Assert
            Assert.True(result);
            byte[] dfu = File.ReadAllBytes(dfuFile);

            // NumberOfTargets in DfuPrefix is at byte 10
            Assert.Equal(1, dfu[10]);

            // ElementAddress of first element
            uint addr = ReadU32LE(dfu, ElementsOffset);
            Assert.Equal(0x08000000u, addr);

            // ElementSize = 16
            uint size = ReadU32LE(dfu, ElementsOffset + 4);
            Assert.Equal(16u, size);
        }

        [Fact]
        public void CreateDfuFile_NonContiguousMemory_ProducesTwoImageElements()
        {
            // Arrange – two separate ELA regions that are non-contiguous
            string hexFile = TempHex("noncontiguous.hex",
                ":020000040800F2",                              // ELA → base 0x08000000
                ":1000000011111111111111111111111111111111E0",  // 16 x 0x11 at 0x08000000
                ":020000041000EA",                              // ELA → base 0x10000000
                ":1000000022222222222222222222222222222222D0",  // 16 x 0x22 at 0x10000000
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "noncontiguous.dfu");

            // Act
            bool result = Hex2Dfu.CreateDfuFile(hexFile, dfuFile);

            // Assert
            Assert.True(result);
            byte[] dfu = File.ReadAllBytes(dfuFile);

            // First element
            uint addr0 = ReadU32LE(dfu, ElementsOffset);
            uint size0 = ReadU32LE(dfu, ElementsOffset + 4);
            Assert.Equal(0x08000000u, addr0);
            Assert.Equal(16u, size0);
            Assert.Equal(0x11, dfu[ElementsOffset + 8]);

            // Second element starts after first element's 8-byte header + data
            int element1Offset = ElementsOffset + 8 + (int)size0;
            uint addr1 = ReadU32LE(dfu, element1Offset);
            uint size1 = ReadU32LE(dfu, element1Offset + 4);
            Assert.Equal(0x10000000u, addr1);
            Assert.Equal(16u, size1);
            Assert.Equal(0x22, dfu[element1Offset + 8]);
        }

        [Fact]
        public void CreateDfuFile_ContiguousDataAcrossMultipleRecords_ProducesOneImageElement()
        {
            // Arrange – two sequential data records (contiguous) under a single ELA
            string hexFile = TempHex("contiguous.hex",
                ":020000040800F2",                              // ELA → base 0x08000000
                ":1000000000000000000000000000000000000000F0",  // bytes 0x00–0x0F
                ":1000100000000000000000000000000000000000E0",  // bytes 0x10–0x1F (contiguous)
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "contiguous.dfu");

            // Act
            bool result = Hex2Dfu.CreateDfuFile(hexFile, dfuFile);

            // Assert
            Assert.True(result);
            byte[] dfu = File.ReadAllBytes(dfuFile);

            uint addr = ReadU32LE(dfu, ElementsOffset);
            uint size = ReadU32LE(dfu, ElementsOffset + 4);
            Assert.Equal(0x08000000u, addr);
            Assert.Equal(32u, size);
        }

        [Fact]
        public void CreateDfuFile_TwoElaBeforeFirstData_ProducesOneNonEmptyImageElement()
        {
            // Regression for empty-ImageElement bug: when a second ELA record shifts
            // offset past ElementAddress before any data is written, the flush condition
            // fires with an empty binDestination and previously created a spurious empty element.
            string hexFile = TempHex("two_ela.hex",
                ":020000040800F2",                              // ELA → base 0x08000000 (sets ElementAddress + offset)
                ":020000040801F1",                              // ELA → base 0x08010000 (offset only, elementAddressSet=true)
                ":1000000011111111111111111111111111111111E0",  // 16 x 0x11 at 0x08010000
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "two_ela.dfu");

            bool result = Hex2Dfu.CreateDfuFile(hexFile, dfuFile);

            Assert.True(result);
            byte[] dfu = File.ReadAllBytes(dfuFile);

            // Exactly one element: address must be the second ELA's base (0x08010000), not the first (0x08000000)
            uint addr = ReadU32LE(dfu, ElementsOffset);
            Assert.Equal(0x08010000u, addr);

            uint size = ReadU32LE(dfu, ElementsOffset + 4);
            Assert.Equal(16u, size);
            Assert.Equal(0x11, dfu[ElementsOffset + 8]);
        }

        [Fact]
        public void CreateDfuFile_SingleElement_BinFileNameHasNoElementSuffix()
        {
            // Regression for double-suffix bug: with one element the name must be
            // "{base}.bin", not "{base}_element_0.bin.bin".
            string hexFile = TempHex("firmware.hex",
                ":020000040800F2",
                ":1000000000000000000000000000000000000000F0",
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "firmware.dfu");

            // bin files land in the test runner's CWD (not in _tempDir) because the
            // code builds the name from Path.GetFileNameWithoutExtension without a directory.
            string cwd = Environment.CurrentDirectory;
            string expectedBin = Path.Combine(cwd, "firmware.bin");
            string wrongBin    = Path.Combine(cwd, "firmware_element_0.bin");
            try
            {
                bool result = Hex2Dfu.CreateDfuFile(hexFile, dfuFile);

                Assert.True(result);
                Assert.True(File.Exists(expectedBin), $"Expected bin file not found: {expectedBin}");
                Assert.False(File.Exists(wrongBin),   $"Unexpected suffixed bin file exists: {wrongBin}");
            }
            finally
            {
                if (File.Exists(expectedBin)) File.Delete(expectedBin);
                if (File.Exists(wrongBin))    File.Delete(wrongBin);
            }
        }

        [Fact]
        public void CreateDfuFile_MultipleElements_BinFileNamesHaveSingleElementSuffix()
        {
            // Regression for double-suffix bug: with N>1 elements the names must be
            // "{base}_element_{i}.bin", not "{base}_element_{i}.bin_element_{i}.bin".
            string hexFile = TempHex("multi.hex",
                ":020000040800F2",
                ":1000000011111111111111111111111111111111E0",
                ":020000041000EA",
                ":1000000022222222222222222222222222222222D0",
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "multi.dfu");

            string cwd = Environment.CurrentDirectory;
            string expectedBin0 = Path.Combine(cwd, "multi_element_0.bin");
            string expectedBin1 = Path.Combine(cwd, "multi_element_1.bin");
            string wrongBin0    = Path.Combine(cwd, "multi_element_0.bin_element_0.bin");
            try
            {
                bool result = Hex2Dfu.CreateDfuFile(hexFile, dfuFile);

                Assert.True(result);
                Assert.True(File.Exists(expectedBin0), $"Expected bin file not found: {expectedBin0}");
                Assert.True(File.Exists(expectedBin1), $"Expected bin file not found: {expectedBin1}");
                Assert.False(File.Exists(wrongBin0),   $"Unexpected double-suffix bin file exists: {wrongBin0}");
            }
            finally
            {
                if (File.Exists(expectedBin0)) File.Delete(expectedBin0);
                if (File.Exists(expectedBin1)) File.Delete(expectedBin1);
                if (File.Exists(wrongBin0))    File.Delete(wrongBin0);
            }
        }

        [Fact]
        public void CreateDfuFile_InvalidHexRecord_ThrowsException()
        {
            // Arrange – a data record with a bad checksum
            string hexFile = TempHex("invalid.hex",
                ":020000040800F2",
                ":1000000000000000000000000000000000000000FF",  // wrong CRC (should be F0)
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "invalid.dfu");

            // Act & Assert
            Assert.Throws<Exception>(() => Hex2Dfu.CreateDfuFile(hexFile, dfuFile));
        }

        [Fact]
        public void CreateDfuFile_ElaSetsBothOffsetAndElementAddress()
        {
            // Arrange – verify that an ELA before any data correctly sets the element base address
            string hexFile = TempHex("ela_addr.hex",
                ":020000041000EA",                              // ELA → base 0x10000000
                ":10000000AABBCCDD00112233445566778899AABB80",  // data at 0x10000000
                ":00000001FF");
            string dfuFile = Path.Combine(_tempDir, "ela_addr.dfu");

            // Act
            bool result = Hex2Dfu.CreateDfuFile(hexFile, dfuFile);

            // Assert
            Assert.True(result);
            byte[] dfu = File.ReadAllBytes(dfuFile);

            uint addr = ReadU32LE(dfu, ElementsOffset);
            Assert.Equal(0x10000000u, addr);

            // Verify data bytes are preserved
            Assert.Equal(0xAA, dfu[ElementsOffset + 8]);
            Assert.Equal(0xBB, dfu[ElementsOffset + 9]);
        }
    }
}
