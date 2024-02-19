namespace RawX
{
    public class WMF
    {
        public List<Mesh> Meshes { get; set; } = new List<Mesh>();
        public List<string> Textures { get; set; } = new List<string>();

        public static WMF Load(string path)
        {
            FileInfo fi = new(path);
            WMF wmf = new();

            using (BinaryReader br = new(fi.OpenRead()))
            {
                Point point;
                uint pointOffset, faceCount, textureOffset, nextBlockOffset;
                uint totalFaceCount = 0;

                do
                {
                    br.ReadBytes(4);
                    pointOffset = br.ReadUInt16();
                    faceCount = br.ReadUInt16();
                    totalFaceCount += faceCount;
                    textureOffset = br.ReadUInt16();
                    br.ReadBytes(8);
                    nextBlockOffset = br.ReadUInt16();

                    Mesh mesh = new Mesh();
                    short stop;

                    do
                    {
                        short x, y, z;

                        x = br.ReadInt16();
                        y = br.ReadInt16();
                        z = br.ReadInt16();
                        point = new Point { X = x, Y = y, Z = z };
                        mesh.Points.Add(point);

                        z = br.ReadInt16();
                        x = br.ReadInt16();
                        y = br.ReadInt16();
                        point = new Point { X = x, Y = y, Z = z };
                        mesh.Points.Add(point);

                        x = br.ReadInt16();
                        y = br.ReadInt16();
                        z = br.ReadInt16();
                        point = new Point { X = x, Y = y, Z = z };
                        mesh.Points.Add(point);

                        stop = br.ReadInt16();
                    } while (stop == 0);

                    for (int i = 0; i < faceCount; i++)
                    {
                        mesh.Faces.Add(new Face
                        {
                            V1 = br.ReadByte(),
                            V2 = br.ReadByte(),
                            V3 = br.ReadByte(),
                            V4 = br.ReadByte()
                        });
                    }

                    br.ReadUInt32();    // 0

                    wmf.Meshes.Add(mesh);
                } while (nextBlockOffset != 0);

                br.ReadBytes(8);    // TEXTURES

                int meshIndex = 0;

                foreach (var mesh in wmf.Meshes)
                    foreach (var face in mesh.Faces)
                    {
                        face.MaterialIndex = (ushort)(br.ReadUInt16() - 1);

                        face.UV1 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                        face.UV2 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                        face.UV3 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                        face.UV4 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                    }

                br.ReadUInt16();    // 0

                while (br.PeekChar() != 0)
                {
                    wmf.Textures.Add(br.ReadString(8));
                }

                br.ReadUInt32();    // 0x00002A2A

                br.ReadBytes(8);    // SHADEMAP

                for (int i = 0; i < totalFaceCount; i++)
                {
                    br.ReadBytes(4);
                }

                br.ReadBytes(8);    // BASEMAPP

                for (int i = 0; i < totalFaceCount; i++)
                {
                    br.ReadBytes(4);
                }

                br.ReadBytes(8);    // ENVMAPPP

                for (int i = 0; i < totalFaceCount; i++)
                {
                    br.ReadBytes(8);
                }
            }

            return wmf;
        }
    }

    public class Mesh
    {
        public List<Point> Points { get; set; } = [];

        public List<Face> Faces { get; set; } = [];

        public Point Offset { get; set; } = new Point();
    }

    public class Point
    {
        public short X { get; set; }

        public short Y { get; set; }

        public short Z { get; set; }
    }

    public class UPoint
    {
        public ushort X { get; set; }

        public ushort Y { get; set; }

        public ushort Z { get; set; }
    }

    public class UV
    {
        public byte U { get; set; }

        public byte V { get; set; }
    }

    public class Face
    {
        public uint V1 { get; set; }
        public UV UV1 { get; set; }

        public uint V2 { get; set; }
        public UV UV2 { get; set; }

        public uint V3 { get; set; }
        public UV UV3 { get; set; }

        public uint V4 { get; set; }
        public UV UV4 { get; set; }

        public ushort MaterialIndex { get; set; }

        public ushort UVIndex { get; set; }

        public ushort XYFlip { get; set; }
    }
}
