using System;

namespace ADNES.Helpers
{
    /// <summary>
    ///     Converts the 8bpp value from the NES 2C02 PPU to a 32bpp BMP image
    ///
    ///     This class is NOT THREAD SAFE (since the NES itself isn't multithreaded), and aims to be as fast as possible while
    ///     reducing allocations and garbage collection.
    /// </summary>
    public static class BitmapHelper
    {
        private const int Height = 240;
        private const int Width = 256;
        private const int HeaderSize = 54;
        private const int BytesPerPixel = 3;
        private static readonly int RowSize = ((Width * BytesPerPixel + 3) / 4) * 4; // Round up to multiple of 4
        private static readonly byte[] ImageBuffer = new byte[54 + ((Width * Height) * BytesPerPixel)]; // Header + 3 bytes per pixel

        static BitmapHelper()
        {
            //Write the BMP header once as it'll be the same for every file
            ImageBuffer[0] = (byte)'B';
            ImageBuffer[1] = (byte)'M';
            BitConverter.GetBytes(ImageBuffer.Length).CopyTo(ImageBuffer, 2);
            ImageBuffer[6] = ImageBuffer[7] = ImageBuffer[8] = ImageBuffer[9] = 0; // Reserved
            BitConverter.GetBytes(54).CopyTo(ImageBuffer, 10); // Offset to image data
            BitConverter.GetBytes(40).CopyTo(ImageBuffer, 14); // Header size
            BitConverter.GetBytes(Width).CopyTo(ImageBuffer, 18); // Width
            BitConverter.GetBytes(Height).CopyTo(ImageBuffer, 22); // Height
            ImageBuffer[26] = 1; // Color planes
            ImageBuffer[28] = 24; // Bits per pixel
            ImageBuffer[30] = ImageBuffer[31] = ImageBuffer[32] = ImageBuffer[33] = 0; // No compression
            BitConverter.GetBytes(ImageBuffer.Length).CopyTo(ImageBuffer, 34); // Image size
            ImageBuffer[38] = ImageBuffer[39] = ImageBuffer[42] = ImageBuffer[43] = 0; // Resolution
            ImageBuffer[46] = ImageBuffer[47] = ImageBuffer[48] = ImageBuffer[49] = 0; // Colors in palette
            ImageBuffer[50] = ImageBuffer[51] = ImageBuffer[52] = ImageBuffer[53] = 0; // Important colors
        }

        /// <summary>
        ///     Converts the 8bpp Bitmap data from the NES PPU to a 32bpp BMP image file
        /// </summary>
        /// <param name="inputBitmap"></param>
        /// <returns></returns>
        public static Span<byte> From8bpp(ReadOnlySpan<byte> inputBitmap) =>
            From32bpp(ColorHelper.GetData(inputBitmap));

        /// <summary>
        ///    Converts the 32bpp Bitmap data to a 32bpp BMP image file
        /// </summary>
        /// <param name="inputBitmap"></param>
        /// <returns></returns>
        public static Span<byte> From32bpp(ReadOnlySpan<byte> inputBitmap)
        {
            var index = HeaderSize;
            var inputBytesPerPixel = 4; // Assuming input is BGRA
            var inputRowSize = Width * inputBytesPerPixel;

            try
            {
                for (var y = Height - 1; y >= 0; y--) // Start from the bottom row
                {
                    var inputRowStart = y * inputRowSize;

                    for (var x = 0; x < Width; x++)
                    {
                        var inputPixelIndex = inputRowStart + x * inputBytesPerPixel;

                        // Write the pixel data in BGR format
                        ImageBuffer[index++] = inputBitmap[inputPixelIndex];
                        ImageBuffer[index++] = inputBitmap[inputPixelIndex + 1];
                        ImageBuffer[index++] = inputBitmap[inputPixelIndex + 2];
                    }

                    // Calculate padding for the current row
                    var padding = RowSize - (Width * BytesPerPixel);

                    // Add padding bytes to the row
                    for (var p = 0; p < padding; p++)
                        ImageBuffer[index++] = 0;
                }

                return ImageBuffer;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
