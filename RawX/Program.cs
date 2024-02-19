using RawX;
using System.Drawing.Imaging;

Dictionary<string, dynamic> imageLookup = [];

string? filePth = args.Where(s => s.EndsWith(".pth", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
string? fileDat = args.Where(s => s.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

if (filePth is null | fileDat is null) 
{
    Console.WriteLine("Both a PTH and a DAT are expected");
    return; 
}

extractPth(filePth, true);

void extractPth(string path, bool keep = false)
{
    Console.WriteLine($"Processing {path}");

    Console.WriteLine($"Creating {Path.GetFileNameWithoutExtension(path)}");

    var destination = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)));

    PTH pth = PTH.Load(path);

    pth.Contents.ForEach(e => File.WriteAllBytes(Path.Combine(destination.FullName, e.Name), pth.Extract(e)));

    foreach (var item in destination.GetFiles("*.pth"))
    {
        extractPth(item.FullName);
    }

    foreach (var item in destination.GetFiles("*.spr"))
    {
        Console.WriteLine($"SPR : {item.Name}");

        SPR spr = SPR.Load(item.FullName);

        imageLookup[spr.Name] = new { X = spr.Width, Y = spr.Height };

        spr.GetBitmap().Save(Path.Combine(destination.FullName, $"{spr.Name}.png"), ImageFormat.Png);

        item.Delete();
    }

    foreach (var item in destination.GetFiles("*.wmf"))
    {
        Console.WriteLine($"WMF : {item.Name}");

        WMF wmf = WMF.Load(item.FullName);

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "mtl")))
        {
            foreach (var texture in wmf.Textures)
            {
                dw.WriteLine($"newmtl {texture}");
                dw.WriteLine("Ka 0.000000 0.000000 0.000000");
                dw.WriteLine("Kd 1.000000 1.000000 1.000000");
                dw.WriteLine("Ks 0.000000 0.000000 0.000000");
                dw.WriteLine($"map_Kd {texture}.png");
            }
        }

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "obj")))
        {
            dw.WriteLine($"mtllib {Path.GetFileNameWithoutExtension(item.FullName)}.MTL");

            int m = 0;
            int v = 0;

            foreach (var mesh in wmf.Meshes)
            {
                dw.WriteLine($"g geometry_{m}");

                foreach (var face in mesh.Faces)
                {
                    string textureName = wmf.Textures[face.MaterialIndex];
                    float width = 127f;
                    float height = 127f;

                    if (imageLookup.TryGetValue(textureName, out dynamic? value))
                    {
                        width = value.X - 1f;
                        height = value.Y - 1f;
                    }

                    dw.WriteLine($"usemtl {wmf.Textures[face.MaterialIndex]}");

                    dw.WriteLine($"vt {face.UV1.U / width} {1f - face.UV1.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V1].X} {mesh.Points[(int)face.V1].Y - mesh.Points[(int)face.V1].Y - mesh.Points[(int)face.V1].Y} {mesh.Points[(int)face.V1].Z} 1");

                    dw.WriteLine($"vt {face.UV2.U / width} {1f - face.UV2.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V2].X} {mesh.Points[(int)face.V2].Y - mesh.Points[(int)face.V2].Y - mesh.Points[(int)face.V2].Y} {mesh.Points[(int)face.V2].Z} 1");

                    dw.WriteLine($"vt {face.UV3.U / width} {1f - face.UV3.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V3].X} {mesh.Points[(int)face.V3].Y - mesh.Points[(int)face.V3].Y - mesh.Points[(int)face.V3].Y} {mesh.Points[(int)face.V3].Z} 1");

                    dw.WriteLine($"vt {face.UV4.U / width} {1f - face.UV4.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V4].X} {mesh.Points[(int)face.V4].Y - mesh.Points[(int)face.V4].Y - mesh.Points[(int)face.V4].Y} {mesh.Points[(int)face.V4].Z} 1");

                    dw.WriteLine($"f {v + 1}/{v + 1} {v + 2}/{v + 2} {v + 3}/{v + 3}");
                    dw.WriteLine($"f {v + 4}/{v + 4} {v + 3}/{v + 3} {v + 2}/{v + 2}");
                    v += 4;
                }

                m++;
            }
        }

        item.Delete();
    }

    foreach (var item in destination.GetFiles("*.wwf"))
    {
        Console.WriteLine($"WWF : {item.Name}");

        WWF wwf = WWF.Load(item.FullName);

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "mtl")))
        {
            foreach (var texture in wwf.Textures)
            {
                dw.WriteLine($"newmtl {texture}");
                dw.WriteLine("Ka 0.000000 0.000000 0.000000");
                dw.WriteLine("Kd 1.000000 1.000000 1.000000");
                dw.WriteLine("Ks 0.000000 0.000000 0.000000");
                dw.WriteLine(@$"map_Kd TEXTURES\{texture}.png");
            }
        }

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "obj")))
        {
            dw.WriteLine($"mtllib {Path.GetFileNameWithoutExtension(item.FullName)}.MTL");

            int m = 0;
            int v = 0;
            int b = 0;

            foreach (var block in wwf.MapBlocks)
            {
                m = 0;
                int tX = 0;
                int tY = 0;
                int tZ = 0;

                foreach (var mesh in block.Meshes)
                {
                    dw.WriteLine($"g BLOCK_{block.X}x{block.Y}_{m}");

                    tX = mesh.Offset.X * 256;
                    tY = -mesh.Offset.Y * 256;
                    tZ = -mesh.Offset.Z * 256;

                    tX += block.X * 16384;
                    tY += -block.Y * 16384;

                    int f = 0;

                    foreach (var face in mesh.Faces)
                    {
                        var entry = wwf.UVTable[face.UVIndex];
                        string textureName = wwf.Textures[entry.MaterialIndex - 1];
                        float width = 127f;
                        float height = 127f;

                        if (imageLookup.TryGetValue(textureName, out dynamic? value))
                        {
                            width = value.X - 1f;
                            height = value.Y - 1f;
                        }

                        dw.WriteLine($"usemtl {textureName}");

                        dw.WriteLine($"vt {entry.UV1.U / width} {1f - entry.UV1.V / height}");
                        dw.WriteLine($"vt {entry.UV2.U / width} {1f - entry.UV2.V / height}");
                        dw.WriteLine($"vt {entry.UV3.U / width} {1f - entry.UV3.V / height}");
                        dw.WriteLine($"vt {entry.UV4.U / width} {1f - entry.UV4.V / height}");

                        dw.WriteLine($"v {mesh.Points[(int)face.V1].X + tX} {-mesh.Points[(int)face.V1].Y + tY} {-mesh.Points[(int)face.V1].Z + tZ} 1");
                        dw.WriteLine($"v {mesh.Points[(int)face.V2].X + tX} {-mesh.Points[(int)face.V2].Y + tY} {-mesh.Points[(int)face.V2].Z + tZ} 1");
                        dw.WriteLine($"v {mesh.Points[(int)face.V3].X + tX} {-mesh.Points[(int)face.V3].Y + tY} {-mesh.Points[(int)face.V3].Z + tZ} 1");
                        dw.WriteLine($"v {mesh.Points[(int)face.V4].X + tX} {-mesh.Points[(int)face.V4].Y + tY} {-mesh.Points[(int)face.V4].Z + tZ} 1");

                        dw.WriteLine($"f {v + 4}/{v + 3} {v + 2}/{v + 2} {v + 1}/{v + 1}");
                        dw.WriteLine($"f {v + 3}/{v + 4} {v + 4}/{v + 3} {v + 1}/{v + 1}");

                        v += 4;
                        f++;
                    }

                    m++;
                }

                b++;
            }

            b = 0;

            foreach (var section in wwf.Sections)
            {
                dw.WriteLine($"g TRACKDAT_{b}");

                m = 0;

                for (int c = 0; c < section.Columns - 1; c++)
                {
                    for (int r = 0; r < section.Rows - 1; r++)
                    {
                        var entry = wwf.UVTable[section.Things[(c + 0) * section.Rows + r + 0].UVIndex];
                        string textureName = wwf.Textures[entry.MaterialIndex - 1];
                        float width = 127f;
                        float height = 127f;

                        if (imageLookup.TryGetValue(textureName, out dynamic? value))
                        {
                            width = value.X - 1f;
                            height = value.Y - 1f;
                        }

                        dw.WriteLine($"usemtl {textureName}");

                        dw.WriteLine($"vt {entry.UV1.U / width} {1f - entry.UV1.V / height}");
                        dw.WriteLine($"vt {entry.UV2.U / width} {1f - entry.UV2.V / height}");
                        dw.WriteLine($"vt {entry.UV3.U / width} {1f - entry.UV3.V / height}");
                        dw.WriteLine($"vt {entry.UV4.U / width} {1f - entry.UV4.V / height}");

                        dw.WriteLine($"v {section.OffsetX + section.OffsetXMultiplier * 65535 + section.Things[(c + 0) * section.Rows + r + 0].P.X} {-section.OffsetY + section.OffsetYMultiplier * -65535 + -section.Things[(c + 0) * section.Rows + r + 0].P.Y} {-section.Things[(c + 0) * section.Rows + r + 0].P.Z} 1");
                        dw.WriteLine($"v {section.OffsetX + section.OffsetXMultiplier * 65535 + section.Things[(c + 1) * section.Rows + r + 0].P.X} {-section.OffsetY + section.OffsetYMultiplier * -65535 + -section.Things[(c + 1) * section.Rows + r + 0].P.Y} {-section.Things[(c + 1) * section.Rows + r + 0].P.Z} 1");
                        dw.WriteLine($"v {section.OffsetX + section.OffsetXMultiplier * 65535 + section.Things[(c + 0) * section.Rows + r + 1].P.X} {-section.OffsetY + section.OffsetYMultiplier * -65535 + -section.Things[(c + 0) * section.Rows + r + 1].P.Y} {-section.Things[(c + 0) * section.Rows + r + 1].P.Z} 1");
                        dw.WriteLine($"v {section.OffsetX + section.OffsetXMultiplier * 65535 + section.Things[(c + 1) * section.Rows + r + 1].P.X} {-section.OffsetY + section.OffsetYMultiplier * -65535 + -section.Things[(c + 1) * section.Rows + r + 1].P.Y} {-section.Things[(c + 1) * section.Rows + r + 1].P.Z} 1");

                        dw.WriteLine($"f {v + 1}/{v + 1} {v + 2}/{v + 2} {v + 4}/{v + 4}");
                        dw.WriteLine($"f {v + 1}/{v + 1} {v + 4}/{v + 4} {v + 3}/{v + 3}");

                        v += 4;
                    }
                }

                b++;
            }
        }

        item.Delete();
    }

    foreach (var item in destination.GetFiles("*.s33"))
    {
        Console.WriteLine($"S33 : {item.Name}");

        S33 s33 = S33.Load(item.FullName);

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "mtl")))
        {
            foreach (var texture in s33.Textures)
            {
                dw.WriteLine($"newmtl {texture}");
                dw.WriteLine("Ka 1.000000 1.000000 1.000000");
                dw.WriteLine("Kd 1.000000 1.000000 1.000000");
                dw.WriteLine("Ks 0.000000 0.000000 0.000000");
                dw.WriteLine(@$"map_Kd {texture}.png");
            }
        }

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "obj")))
        {
            dw.WriteLine($"mtllib {Path.GetFileNameWithoutExtension(item.FullName)}.MTL");

            int m = 0;
            int v = 0;

            foreach (var mesh in s33.Meshes)
            {
                dw.WriteLine($"g geometry_{m}");

                foreach (var face in mesh.Faces)
                {
                    string textureName = s33.Textures[face.MaterialIndex];
                    float width = 127f;
                    float height = 127f;

                    if (imageLookup.TryGetValue(textureName, out dynamic? value))
                    {
                        width = value.X - 1f;
                        height = value.Y - 1f;
                    }

                    dw.WriteLine($"usemtl {textureName}");

                    dw.WriteLine($"vt {face.UV1.U / width} {1f - face.UV1.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V1].X} {-mesh.Points[(int)face.V1].Y} {-mesh.Points[(int)face.V1].Z} 1");

                    dw.WriteLine($"vt {face.UV2.U / width} {1f - face.UV2.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V2].X} {-mesh.Points[(int)face.V2].Y} {-mesh.Points[(int)face.V2].Z} 1");

                    dw.WriteLine($"vt {face.UV3.U / width} {1f - face.UV3.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V3].X} {-mesh.Points[(int)face.V3].Y} {-mesh.Points[(int)face.V3].Z} 1");

                    dw.WriteLine($"vt {face.UV4.U / width} {1f - face.UV4.V / height}");
                    dw.WriteLine($"v {mesh.Points[(int)face.V4].X} {-mesh.Points[(int)face.V4].Y} {-mesh.Points[(int)face.V4].Z} 1");

                    dw.WriteLine($"f {v + 3}/{v + 3} {v + 2}/{v + 2} {v + 1}/{v + 1}");
                    dw.WriteLine($"f {v + 2}/{v + 2} {v + 3}/{v + 3} {v + 4}/{v + 4}");
                    v += 4;
                }

                m++;
            }
        }

        item.Delete();
    }

    if (!keep)
    {
        File.Delete(Path.ChangeExtension(path, "dat"));
        File.Delete(path);
    }
}