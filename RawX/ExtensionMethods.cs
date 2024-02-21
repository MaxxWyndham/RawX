using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;

namespace RawX
{
    public static class ExtensionMethods
    {
        public static string ReadString(this BinaryReader br, int length)
        {
            if (length == 0) { return null; }

            char[] c = br.ReadChars(length);
            int l = length;

            for (int i = 0; i < length; i++)
            {
                if (c[i] == 0)
                {
                    l = i;
                    break;
                }
            }

            return new string(c, 0, l);
        }

        public static string ReadNullTerminatedString(this BinaryReader br)
        {
            string r = "";
            char c = (char)0;

            do
            {
                c = br.ReadChar();
                if (c > 0) { r += c; }
            } while (c > 0);

            return r;
        }

        public static Bitmap Resize(this Bitmap b, int width, int height)
        {
            if (b.PixelFormat == PixelFormat.Format8bppIndexed) { return b; }

            Bitmap d = new(width, height);

            using (Graphics graphics = Graphics.FromImage(d))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(b, new Rectangle(0, 0, width, height), 0, 0, b.Width, b.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return d;
        }
    }
}
