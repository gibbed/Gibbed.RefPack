namespace Gibbed.RefPack
{
    public static class ByteHelpers
    {
        public static bool RefPackCompress(this byte[] input, out byte[] output, CompressionLevel level)
        {
            return Compression.Compress(input, out output, level);
        }

        public static bool RefPackCompress(this byte[] input, out byte[] output)
        {
            return Compression.Compress(input, out output, CompressionLevel.Max);
        }

        public static byte[] RefPackDecompress(this byte[] input)
        {
            return Decompression.Decompress(input);
        }
    }
}
