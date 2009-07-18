using System.IO;

namespace Gibbed.RefPack
{
    public static class StreamHelpers
    {
        public static bool RefPackCompress(this Stream input, int length, out byte[] output, CompressionLevel level)
        {
            byte[] data = new byte[length];
            input.Read(data, 0, data.Length);
            return Compression.Compress(data, out output, level);
        }

        public static bool RefPackCompress(this Stream input, int length, out byte[] output)
        {
            byte[] data = new byte[length];
            input.Read(data, 0, data.Length);
            return Compression.Compress(data, out output, CompressionLevel.Max);
        }

        public static byte[] RefPackDecompress(this Stream input)
        {
            return Decompression.Decompress(input);
        }
    }
}
