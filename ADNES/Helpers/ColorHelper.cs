using System;
using System.Drawing;

namespace ADNES.Helpers
{
    /// <summary>
    ///     This is a helper class that can concert the 8-bit value from the NES 2C02 PPU to an RGB Color
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        ///    The NES 2C02 Color Palette mapped to System.Drawing.Color
        ///
        ///     Array offset is the 8-bit value from the NES 2C02 PPU
        /// </summary>
        public static readonly Color[] ColorPalette = new Color[0x40];

        /// <summary>
        ///     Buffer for a full frame of 8-bit NES color data as 24-bit BMP color data
        /// </summary>
        public static readonly byte[] FrameBuffer = new byte[256 * 240 * 4];

        static ColorHelper()
        {
            ColorPalette[0x0] = Color.FromArgb(84, 84, 84);
            ColorPalette[0x1] = Color.FromArgb(0, 30, 116);
            ColorPalette[0x2] = Color.FromArgb(8, 16, 144);
            ColorPalette[0x3] = Color.FromArgb(48, 0, 136);
            ColorPalette[0x4] = Color.FromArgb(68, 0, 100);
            ColorPalette[0x5] = Color.FromArgb(92, 0, 48);
            ColorPalette[0x6] = Color.FromArgb(84, 4, 0);
            ColorPalette[0x7] = Color.FromArgb(60, 24, 0);
            ColorPalette[0x8] = Color.FromArgb(32, 42, 0);
            ColorPalette[0x9] = Color.FromArgb(8, 58, 0);
            ColorPalette[0xa] = Color.FromArgb(0, 64, 0);
            ColorPalette[0xb] = Color.FromArgb(0, 60, 0);
            ColorPalette[0xc] = Color.FromArgb(0, 50, 60);
            ColorPalette[0xd] = Color.FromArgb(0, 0, 0);
            ColorPalette[0xe] = Color.FromArgb(0, 0, 0);
            ColorPalette[0xf] = Color.FromArgb(0, 0, 0);
            ColorPalette[0x10] = Color.FromArgb(152, 150, 152);
            ColorPalette[0x11] = Color.FromArgb(8, 76, 196);
            ColorPalette[0x12] = Color.FromArgb(48, 50, 236);
            ColorPalette[0x13] = Color.FromArgb(92, 30, 228);
            ColorPalette[0x14] = Color.FromArgb(136, 20, 176);
            ColorPalette[0x15] = Color.FromArgb(160, 20, 100);
            ColorPalette[0x16] = Color.FromArgb(152, 34, 32);
            ColorPalette[0x17] = Color.FromArgb(120, 60, 0);
            ColorPalette[0x18] = Color.FromArgb(84, 90, 0);
            ColorPalette[0x19] = Color.FromArgb(40, 114, 0);
            ColorPalette[0x1a] = Color.FromArgb(8, 124, 0);
            ColorPalette[0x1b] = Color.FromArgb(0, 118, 40);
            ColorPalette[0x1c] = Color.FromArgb(0, 102, 120);
            ColorPalette[0x1d] = Color.FromArgb(0, 0, 0);
            ColorPalette[0x1e] = Color.FromArgb(0, 0, 0);
            ColorPalette[0x1f] = Color.FromArgb(0, 0, 0);
            ColorPalette[0x20] = Color.FromArgb(236, 238, 236);
            ColorPalette[0x21] = Color.FromArgb(76, 154, 236);
            ColorPalette[0x22] = Color.FromArgb(120, 124, 236);
            ColorPalette[0x23] = Color.FromArgb(176, 98, 236);
            ColorPalette[0x24] = Color.FromArgb(228, 84, 236);
            ColorPalette[0x25] = Color.FromArgb(236, 88, 180);
            ColorPalette[0x26] = Color.FromArgb(236, 106, 100);
            ColorPalette[0x27] = Color.FromArgb(212, 136, 32);
            ColorPalette[0x28] = Color.FromArgb(160, 170, 0);
            ColorPalette[0x29] = Color.FromArgb(116, 196, 0);
            ColorPalette[0x2a] = Color.FromArgb(76, 208, 32);
            ColorPalette[0x2b] = Color.FromArgb(56, 204, 108);
            ColorPalette[0x2c] = Color.FromArgb(56, 180, 204);
            ColorPalette[0x2d] = Color.FromArgb(60, 60, 60);
            ColorPalette[0x2e] = Color.FromArgb(0, 0, 0);
            ColorPalette[0x2f] = Color.FromArgb(0, 0, 0);
            ColorPalette[0x30] = Color.FromArgb(236, 238, 236);
            ColorPalette[0x31] = Color.FromArgb(168, 204, 236);
            ColorPalette[0x32] = Color.FromArgb(188, 188, 236);
            ColorPalette[0x33] = Color.FromArgb(212, 178, 236);
            ColorPalette[0x34] = Color.FromArgb(236, 174, 236);
            ColorPalette[0x35] = Color.FromArgb(236, 174, 212);
            ColorPalette[0x36] = Color.FromArgb(236, 180, 176);
            ColorPalette[0x37] = Color.FromArgb(228, 196, 144);
            ColorPalette[0x38] = Color.FromArgb(204, 210, 120);
            ColorPalette[0x39] = Color.FromArgb(180, 222, 120);
            ColorPalette[0x3a] = Color.FromArgb(168, 226, 144);
            ColorPalette[0x3b] = Color.FromArgb(152, 226, 180);
            ColorPalette[0x3c] = Color.FromArgb(160, 214, 228);
            ColorPalette[0x3d] = Color.FromArgb(160, 162, 160);
            ColorPalette[0x3e] = Color.FromArgb(0, 0, 0);
            ColorPalette[0x3f] = Color.FromArgb(0, 0, 0);
        }

        /// <summary>
        ///     Takes in an 8-bit value representing a color in the NES 2C02 color palette and returns a System.Drawing.Color
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Color GetColor(byte value) => ColorPalette[value];

        /// <summary>
        ///     Takes in an 8-bit value representing a color in the NES 2C02 color palette and returns a byte[] representing the
        ///     color in Argb format
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] GetData(byte value)
        {
            var color = ColorPalette[value];
            return [color.B, color.G, color.R, 255];
        }

        /// <summary>
        ///     This method takes in the 8bpp bitmap data from the NES Emulator and converts it using the mapped color palette to a
        ///     bitmap representation in a byte[] that can later be used in the WriteableBitmap.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> GetData(ReadOnlySpan<byte> data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                var color = ColorPalette[data[i]];
                FrameBuffer[i * 4] = color.B;
                FrameBuffer[i * 4 + 1] = color.G;
                FrameBuffer[i * 4 + 2] = color.R;
                FrameBuffer[i * 4 + 3] = 255;
            }

            return FrameBuffer;
        }
    }
}
