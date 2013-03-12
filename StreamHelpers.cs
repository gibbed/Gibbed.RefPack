/* Copyright (c) 2013 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Diagnostics;
using System.IO;

namespace Gibbed.RefPack
{
    public static class StreamHelpers
    {
        public static bool RefPackCompress(this Stream input, int length, out byte[] output, CompressionLevel level)
        {
            var data = new byte[length];
            input.Read(data, 0, data.Length);
            return Compression.Compress(data, out output, level);
        }

        public static bool RefPackCompress(this Stream input, int length, out byte[] output)
        {
            var data = new byte[length];
            input.Read(data, 0, data.Length);
            return Compression.Compress(data, out output, CompressionLevel.Max);
        }

        public static byte[] RefPackDecompress(this Stream input)
        {
            return Decompression.Decompress(input);
        }

        internal static UInt32 ReadValueU24(this Stream stream)
        {
            var data = new byte[4];
            int read = stream.Read(data, 0, 3);
            Debug.Assert(read == 3);
            return BitConverter.ToUInt32(data, 0) & 0xFFFFFF;
        }

        internal static void WriteValueU24(this Stream stream, UInt32 value)
        {
            var data = BitConverter.GetBytes(value & 0xFFFFFF);
            Debug.Assert(data.Length == 4);
            stream.Write(data, 0, 3);
        }
    }
}
