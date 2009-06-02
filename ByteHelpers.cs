namespace Gibbed.RefPack
{
    public static partial class ByteHelpers
    {
        public static byte[] RefPackCompress(this byte[] input)
        {
            return Compression.Compress(input);
        }

        public static byte[] RefPackDecompress(this byte[] input)
        {
            return Decompression.Decompress(input);
        }
    }
}
