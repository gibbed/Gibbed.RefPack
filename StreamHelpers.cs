using System;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.RefPack
{
    public static class StreamHelpers
    {
        public static byte[] RefPackCompress(this Stream input, int length)
        {
            return Compression.Compress(input, length);
        }

        public static byte[] RefPackDecompress(this Stream input)
        {
            return Decompression.Decompress(input);
        }
    }
}
