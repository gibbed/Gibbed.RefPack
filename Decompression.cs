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
using System.IO;
using Gibbed.IO;

namespace Gibbed.RefPack
{
    public class Decompression
    {
        public static byte[] Decompress(byte[] input)
        {
            using (var data = new MemoryStream())
            {
                data.Write(input, 0, input.Length);
                data.Seek(0, SeekOrigin.Begin);
                return data.RefPackDecompress();
            }
        }

        public static byte[] Decompress(Stream input)
        {
            var header = input.ReadValueU16(Endian.Big);
            if ((header & 0x1FFF) != 0x10FB)
            {
                throw new InvalidOperationException("input is not compressed");
            }

            var isLong = (header & 0x8000) != 0;
            var isDoubled = (header & 0x0100) != 0;

            if (isDoubled == true)
            {
                throw new InvalidOperationException("this should never happen");
            }

            uint decompressedSize = isLong ? input.ReadValueU32(Endian.Big) : input.ReadValueU24();

            var data = new byte[decompressedSize];
            uint offset = 0;

            while (true)
            {
                bool stop = false;
                uint plainSize;
                var copySize = 0u;
                var copyOffset = 0u;

                byte prefix = input.ReadValueU8();
                if (prefix < 0x80)
                {
                    var extra = input.ReadValueU8();

                    plainSize = (UInt32)(prefix & 0x03);
                    copySize = (UInt32)(((prefix & 0x1C) >> 2) + 3);
                    copyOffset = (UInt32)((((prefix & 0x60) << 3) | extra) + 1);
                }
                else if (prefix < 0xC0)
                {
                    var extra = new byte[2];
                    input.Read(extra, 0, extra.Length);

                    plainSize = (uint)(extra[0] >> 6);
                    copySize = (uint)((prefix & 0x3F) + 4);
                    copyOffset = (uint)((((extra[0] & 0x3F) << 8) | extra[1]) + 1);
                }
                else if (prefix < 0xE0)
                {
                    var extra = new byte[3];
                    input.Read(extra, 0, extra.Length);

                    plainSize = (uint)(prefix & 3);
                    copySize = (uint)((((prefix & 0x0C) << 6) | extra[2]) + 5);
                    copyOffset = (uint)((((((prefix & 0x10) << 4) | extra[0]) << 8) | extra[1]) + 1);
                }
                else if (prefix < 0xFC)
                {
                    plainSize = (uint)(((prefix & 0x1F) + 1) * 4);
                }
                else
                {
                    plainSize = (uint)(prefix & 3);
                    stop = true;
                }

                if (plainSize > 0)
                {
                    input.Read(data, (int)offset, (int)plainSize);
                    offset += plainSize;
                }

                if (copySize > 0)
                {
                    for (uint i = 0; i < copySize; i++)
                    {
                        data[offset + i] = data[(offset - copyOffset) + i];
                    }

                    offset += copySize;
                }

                if (stop)
                {
                    break;
                }
            }

            return data;
        }
    }
}
