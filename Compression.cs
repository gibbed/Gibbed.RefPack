using System;
using System.Diagnostics;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.RefPack
{
    public class Compression
    {
        public static byte[] Compress(Stream input, int length)
        {
            byte[] data = new byte[length];
            input.Read(data, 0, data.Length);
            return Compress(data);
        }

        public static byte[] Compress(byte[] input)
        {
            if (input.LongLength > 0xFFFFFFFF)
            {
                throw new InvalidOperationException("data to be compressed is too large");
            }

            UInt16 header = 0x10FB;

            if (input.Length > 0xFFFFFF)
            {
                header |= 0x8000;
            }

            MemoryStream work = new MemoryStream();
            work.WriteU16(header, false);

            if (input.Length > 0x00FFFFFF)
            {
                work.WriteU32((UInt32)input.Length, false);
            }
            else
            {
                work.WriteU24((UInt32)input.Length, false);
            }

            int aligned = (input.Length >> 2) << 2;
            int position = 0;

            while (position < aligned)
            {
                int block = position < 4 ? 4 : 0;
                int clength = 3;

                int hit = Search(input, position + block, clength, -1);
                while (hit == -1 && block < 112 && position + block < aligned)
                {
                    block++;
                    hit = Search(input, position + block, clength, -1);
                }

                int cposition = hit;
                if (hit != -1)
                {
                    while (clength + 1 < 1028 && clength + 1 + cposition < position + block)
                    {
                        hit = Search(input, position + block, clength + 1, cposition);
                        if (hit == -1)
                        {
                            break;
                        }
                        
                        clength++;
                        cposition = hit;
                    }
                }

                int precomp = block & 3;
                block -= precomp;

                if (block > 0)
                {
                    block = WriteChunk(input, position, block, -1, 0, work);
                }

                if (precomp != 0 || cposition != -1)
                {
                    block += WriteChunk(input, position + block, precomp, cposition, cposition == -1 ? 0 : clength, work);
                }

                position += block;
            }

            if (position < input.Length)
            {
                position += WriteChunk(input, position, input.Length - position, -1, 0, work);
            }

            Debug.Assert(position == input.Length);

            work.Seek(0, SeekOrigin.Begin);
            byte[] output = new byte[work.Length];
            work.Read(output, 0, output.Length);
            return output;
        }

        private static int Search(byte[] data, int kposition, int klength, int sstart)
        {
            Debug.Assert(kposition >= klength);
            Debug.Assert(kposition + klength <= data.Length);

            if (sstart == -1)
            {
                sstart = kposition - klength;
            }

            Debug.Assert(sstart + klength <= kposition);

            int limit = klength < 4 ? 0x400 : (klength < 5 ? 0x4000 : 0x20000);

        retry:
            /*find first byte*/
            while (data[sstart] != data[kposition]) /*not found*/
            {
                if (sstart == 0 || kposition - sstart == limit) return -1;
                sstart--;
            }

            /*found first byte; check remainder*/
            for (int i = 1; i < klength; i++)
            {
                if (data[sstart + i] == data[kposition + i]) continue; /*found*/
                if (sstart == 0 || kposition - sstart == limit) return -1; /*out of data*/
                sstart--;
                goto retry;
            }
            return sstart;
        }

        private static int WriteChunk(byte[] input, int uposition, int ulength, int cposition, int clength, Stream output)
        {
            Debug.Assert(uposition + ulength <= input.Length);

            byte packing = 0;
            byte[] param = null;

            int written = ulength + clength;

            if (cposition == -1)
            {
                Debug.Assert(ulength <= 112);
                Debug.Assert(clength == 0);

                if (ulength > 3)
                {
                    Debug.Assert((ulength & 3) == 0);
                    packing = (byte)((ulength >> 2) - 1); //00000000 - 01110000 >> 00000000 - 00011011
                    packing |= 0xE0; // 000aaaaa >> 111aaaaa
                }
                else // Should only happen at end of file
                {
                    packing = (byte)ulength;//(uncsize & 0x03)
                    packing |= 0xFC;
                }
            }
            else
            {
                int offset = uposition + ulength - cposition - 1;

                Debug.Assert(cposition < uposition + ulength);
                Debug.Assert(cposition + clength <= uposition + ulength);
                Debug.Assert(offset < 0x20000);
                Debug.Assert(offset + 1 <= uposition + ulength);
                Debug.Assert(ulength <= 3);

                if (offset < 0x400 && clength < 11)
                {
                    param = new byte[1];

                    param[0] = (byte)(offset & 0xFF);
                    packing = (byte)((offset & 0x300) >> 3); // aa ........ >> 0aa.....

                    clength -= 3;
                    
                    packing |= (byte)((clength & 0x07) << 2); // .....bbb >> ...bbb..
                    packing |= (byte)(ulength & 0x03); // >> ......cc
                }
                else if (offset < 0x4000 && clength < 68)
                {
                    param = new byte[2];

                    param[0] = (byte)((offset & 0x3f00) >> 8);
                    param[0] |= (byte)((ulength & 0x03) << 6);
                    param[1] = (byte)(offset & 0xFF);

                    clength -= 4;

                    packing = (byte)(clength & 0x3F);
                    packing |= 0x80;
                }
                else
                {
                    param = new byte[3];

                    packing = (byte)((offset & 0x10000) >> 12);
                    param[0] = (byte)((offset & 0x0FF00) >> 8);
                    param[1] = (byte)(offset & 0x000FF);

                    clength -= 5;

                    packing |= (byte)((clength & 0x300) >> 6);
                    param[2] = (byte)(clength & 0x0FF);

                    packing |= (byte)(ulength & 0x03);
                    packing |= 0xC0;
                }
            }

            output.WriteU8(packing);

            if (param != null)
            {
                output.Write(param, 0, param.Length);
            }

            if (ulength > 0)
            {
                output.Write(input, uposition, ulength);
            }

            return written;
        }
    }
}
