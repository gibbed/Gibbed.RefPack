using System;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.RefPack
{
    public class Decompression
    {
        public static byte[] Decompress(byte[] input)
        {
            MemoryStream data = new MemoryStream();
            data.Write(input, 0, input.Length);
            data.Seek(0, SeekOrigin.Begin);
            return data.RefPackDecompress();
        }

        public static byte[] Decompress(Stream input)
        {
            UInt16 header = input.ReadValueU16(false);
            if ((header & 0x1FFF) != 0x10FB)
            {
                throw new InvalidOperationException("input is not compressed");
            }

            bool isLong = (header & 0x8000) == 0x8000;
            bool isDoubled = (header & 0x0100) == 0x0100;

            if (isDoubled == true)
            {
                throw new InvalidOperationException("this should never happen");
            }

            UInt32 decompressedSize = isLong ? input.ReadValueU32(false) : input.ReadValueU24(false);

            long baseOffset = input.Position;
            byte[] data = new byte[decompressedSize];
            uint offset = 0;

            while (true)
            {
                bool stop = false;
                UInt32 plainSize = 0;
                UInt32 copySize = 0;
                UInt32 copyOffset = 0;

                byte prefix = input.ReadValueU8();

                if (prefix < 0x80)
                {
                    byte extra = input.ReadValueU8();
                    
                    plainSize = (UInt32)(prefix & 0x03);
                    copySize = (UInt32)(((prefix & 0x1C) >> 2) + 3);
                    copyOffset = (UInt32)((((prefix & 0x60) << 3) | extra) + 1);
                }
                else if (prefix < 0xC0)
                {
                    byte[] extra = new byte[2];
                    input.Read(extra, 0, extra.Length);

                    plainSize = (uint)(extra[0] >> 6);
                    copySize = (uint)((prefix & 0x3F) + 4);
                    copyOffset = (uint)((((extra[0] & 0x3F) << 8) | extra[1]) + 1);
                }
                else if (prefix < 0xE0)
                {
                    byte[] extra = new byte[3];
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
