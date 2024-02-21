using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RawX
{
    public class SPR
    {
        public enum ChannelOrder
        {
            BGR,
            RGB
        }

        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public List<Color> Colours { get; set; } = new List<Color>();

        public byte[] Data { get; set; }

        public static SPR Load(string path)
        {


        Color PSX5551ToColor(int pixel, ChannelOrder order = ChannelOrder.BGR, bool useTransparency = false)
            {
                int alpha = 255;
                int special = (pixel & 0x8000) >> 15;
                int r = ((pixel & 0x7c00) >> 10) << 3;
                int g = ((pixel & 0x03e0) >> 5) << 3;
                int b = (pixel & 0x001f) << 3;

                if (useTransparency && r == 0 && g == 0 && b == 0) { alpha = 0; }

                if (order == ChannelOrder.BGR)
                {
                    int x = r;
                    r = b;
                    b = x;
                }

                return Color.FromArgb(alpha, r, g, b);
            }

            FileInfo fi = new(path);
            SPR spr = new()
            {
                Name = Path.GetFileNameWithoutExtension(path)
            };

            using (BinaryReader br = new(fi.OpenRead()))
            {
                br.ReadUInt16();    // ?
                byte mode = br.ReadByte();
                br.ReadByte();      // ?
                br.ReadUInt16();    // ?
                br.ReadUInt16();    // ?

                int colours = mode == 1 ? 16 : 256;

                for (int i = 0; i < colours; i++)
                {
                    spr.Colours.Add(PSX5551ToColor(br.ReadUInt16(), ChannelOrder.BGR, i == 0));
                }

                br.ReadUInt16();    // ?
                spr.Width = br.ReadUInt16();
                spr.Height = br.ReadUInt16();
                br.ReadUInt16();    // ?

                spr.Data = new byte[spr.Width * spr.Height];

                int pointer = 0;

                for (int y = 0; y < spr.Height; y++)
                {
                    for (int x = 0; x < spr.Width; x++)
                    {
                        byte i = br.ReadByte();

                        if (mode == 1)
                        {
                            spr.Data[pointer++] = (byte)(i & 0xf);
                            if (x + 1 < spr.Width) { spr.Data[pointer++] = (byte)(i >> 4); }
                            //spr.Data[pointer++] = (byte)(i >> 4);
                        }
                        else
                        {
                            spr.Data[pointer++] = i;
                        }

                        if (mode == 1) { x++; }
                    }

                    if (spr.Width % 4 > 0 && spr.Width % 2 == 0) { br.ReadByte(); }
                }
            }

            return spr;
        }

        public Bitmap GetBitmap()
        {
            Bitmap bmp = new(Width, Height, PixelFormat.Format32bppArgb);
            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            using (MemoryStream nms = new())
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Color c = Colours[Data[y * Width + x]];
                        nms.WriteByte(c.B);
                        nms.WriteByte(c.G);
                        nms.WriteByte(c.R);
                        nms.WriteByte(c.A);
                    }
                }

                byte[] contentBuffer = new byte[nms.Length];
                nms.Position = 0;
                nms.Read(contentBuffer, 0, contentBuffer.Length);

                Marshal.Copy(contentBuffer, 0, bmpdata.Scan0, contentBuffer.Length);
            }

            bmp.UnlockBits(bmpdata);

            return bmp;
        }
    }
}
