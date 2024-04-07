namespace RawX
{
    public class InstsDAT
    {
        public List<Mesh> Meshes { get; set; } = [];

        public List<Instance> Instances { get; set; } = [];

        public List<string> Textures { get; set; } = [];

        public static InstsDAT Load(string path)
        {
            FileInfo fi = new(path);
            InstsDAT insts = new();

            using (BinaryReader br = new(fi.OpenRead()))
            {
                while (br.Peek() != 0)
                {
                    Instance instance = new()
                    {
                        A = br.ReadByte(),
                        X = br.ReadUInt16(),
                        XMultiplier = br.ReadSByte(),
                        C = br.ReadByte(),
                        Y = br.ReadUInt16(),
                        YMultiplier = br.ReadSByte(),
                        Z = br.ReadInt16(),
                        Offset = br.ReadUInt16(),
                        E = br.ReadUInt32(),
                        F = br.ReadByte(),
                        G = br.ReadUInt16(),
                        H = br.ReadByte(),
                        I = br.ReadUInt32()
                    };

                    instance.X += instance.XMultiplier * 0xFFFF;
                    instance.Y += instance.YMultiplier * 0xFFFF;

                    insts.Instances.Add(instance);
                }

                br.ReadUInt32();    // 0

                long startOfOffsets = br.BaseStream.Position;
                List<uint> offsets = [];

                uint offset = br.ReadUInt32();

                while (offset > 0)
                {
                    offsets.Add(offset);
                    offset = br.ReadUInt32();
                }

                Point point;
                uint pointOffset, faceCount, nextBlockOffset, textureOffset;
                uint totalFaceCount = 0;
                long startOfBlock;

                for (int i = 0; i < offsets.Count; i++)
                {
                    br.BaseStream.Seek(startOfOffsets + offsets[i] + (i * 4), SeekOrigin.Begin);

                    startOfBlock = br.BaseStream.Position;

                    pointOffset = br.ReadUInt16();
                    faceCount = br.ReadUInt16();
                    totalFaceCount += faceCount;
                    textureOffset = br.ReadUInt16();
                    br.ReadBytes(8);
                    nextBlockOffset = br.ReadUInt16();

                    Mesh mesh = new();
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

                    br.BaseStream.Seek(startOfBlock + pointOffset, SeekOrigin.Begin);

                    for (int x = 0; x < faceCount; x++)
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

                    foreach (var face in mesh.Faces)
                    {
                        face.MaterialIndex = (ushort)(br.ReadByte() - 1);
                        face.XYFlip = br.ReadByte();
                        face.UV1 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                        face.UV2 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                        face.UV3 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                        face.UV4 = new UV { U = br.ReadByte(), V = br.ReadByte() };
                    }

                    insts.Meshes.Add(mesh);
                }
            }

            return insts;
        }
    }

    public class Instance
    {
        public byte A { get; set; }

        public int X { get; set; }

        public sbyte XMultiplier { get; set; }

        public byte C { get; set; }

        public int Y { get; set; }

        public sbyte YMultiplier { get; set; }

        public int Z { get; set; }

        public ushort Offset { get; set; }

        public uint E { get; set; }

        public byte F { get; set; }

        public ushort G { get; set; }

        public byte H { get; set; }

        public uint I { get; set; }
    }
}
