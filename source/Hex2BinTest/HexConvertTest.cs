//
// Copyright (c) 2021 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using Xunit;
using nanoFramework.Tools;

namespace Hex2BinTest
{
    public class HexConvertTest
    {
        [Theory]
        [InlineData("123456")]
        [InlineData(":123456")]
        [InlineData(":107FA800000080011000C0FFFDFF58003AFFBBF144")]
        public void TestInvalidLine(string notValid)
        {
            // Arrange
            // Act
            HexFormat hex = new HexFormat(notValid);
            //Assert
            Assert.False(hex.IsValidRecord);
        }

        [Theory]
        [InlineData(":10D4A400002F0020000000000000000000330020D6")]
        [InlineData(":04D4B4000000000074")]
        [InlineData(":08D4B800CC36FC7F01000000EE")]
        [InlineData(":020000025000AC")]
        [InlineData(":107FA800000080011000C0FFFDFF58003AFFBBF140")]
        [InlineData(":107FB800FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFC9")]
        [InlineData(":00000001FF")]
        [InlineData(":04000003300020F1B8")]
        [InlineData(":10D314000000000070180020000000000000000061")]
        [InlineData(":1004C00006EE903A36EEC77A37EE647AA5EEC57A34")]
        public void TestFewValidEntries(string valid)
        {
            // Arrange
            // Act
            HexFormat hex = new HexFormat(valid);
            //Assert
            Assert.True(hex.IsValidRecord);
        }

        [Fact]
        public void TestEndOfFile()
        {
            // Arrange
            string endoffile = ":00000001FF";
            // Act
            HexFormat hex = new HexFormat(endoffile);
            // Assert
            Assert.True(hex.IsValidRecord);
            Assert.Equal(HexFieldType.EndOfFile, hex.HexFieldType);
            Assert.Equal(0, hex.NumberOfBytes);
            Assert.Equal(0, hex.Data.Length);
        }

        [Fact]
        public void TestExtendedAddress()
        {
            // Arrange
            string extended = ":020000025000AC";
            // Act
            HexFormat hex = new HexFormat(extended);
            // Assert
            Assert.True(hex.IsValidRecord);
            Assert.Equal(HexFieldType.ExtendedSegmentAddress, hex.HexFieldType);
        }

    }
}
