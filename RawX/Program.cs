using RawX;
using System.Drawing.Imaging;
using System.Numerics;

Dictionary<string, dynamic> imageLookup = [];

Settings settings = Settings.Load("settings.json");

if (args.Length == 0)
{
    foreach (var item in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.pth"))
    {
        extractPth(item.FullName, true);
    }
}
else if (args.Length == 1 && args[0].Equals("--config", StringComparison.CurrentCultureIgnoreCase))
{
    while (true)
    {
        Console.Clear();

        Console.WriteLine("RawX v1.5");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(" F");
        Console.ResetColor();
        Console.Write("orce power of two textures: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(settings.ForcePowerOfTwo);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(" K");
        Console.ResetColor();
        Console.Write("eep processed files: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(settings.KeepProcessedFiles);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(" S");
        Console.ResetColor();
        Console.Write("cale factor: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(settings.ScaleFactor);
        Console.ResetColor();

        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(" Q");
        Console.ResetColor();
        Console.Write("uit");

        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine("Press a key...");

        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.F:
                settings.ForcePowerOfTwo = !settings.ForcePowerOfTwo;
                settings.Save("settings.json");
                break;

            case ConsoleKey.K:
                settings.KeepProcessedFiles = !settings.KeepProcessedFiles;
                settings.Save("settings.json");
                break;

            case ConsoleKey.S:
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("Please input a scale factor (eg, 0.135) and press enter to finish:");
                    string scaleFactor = Console.ReadLine();
                    if (!float.TryParse(scaleFactor, out float scaleFactorF))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"{scaleFactor} is not a valid float!");
                        Console.ResetColor();
                    }
                    else
                    {
                        settings.ScaleFactor = scaleFactorF;
                        settings.Save("settings.json");
                        break;
                    }
                }
                break;

            case ConsoleKey.Q:
                return;
        }
    }
}
else
{
    string? filePth = args.Where(s => s.EndsWith(".pth", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
    string? fileDat = args.Where(s => s.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

    if (filePth is null | fileDat is null)
    {
        Console.WriteLine("Both a PTH and a DAT are expected");
        return;
    }

    extractPth(filePth, true);
}

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

        var bitmap = spr.GetBitmap();

        if (settings.ForcePowerOfTwo)
        {
            bool resize = false;
            int newWidth = spr.Width;
            int newHeight = spr.Height;

            if (!BitOperations.IsPow2(spr.Width))
            {
                newWidth = 1;
                while (newWidth < spr.Width) { newWidth <<= 1; }
                resize = true;
            }

            if (!BitOperations.IsPow2(spr.Height))
            {
                newHeight = 1;
                while (newHeight < spr.Height) { newHeight <<= 1; }
                resize = true;
            }

            if (resize)
            {
                Console.WriteLine($"Resizing {spr.Width}x{spr.Height} to {newWidth}x{newHeight}");

                bitmap = bitmap.Resize(newWidth, newHeight);
            }
        }

        bitmap.Save(Path.Combine(destination.FullName, $"{spr.Name}.png"), ImageFormat.Png);

        if (!settings.KeepProcessedFiles)
        {
            Console.WriteLine($"Deleting {item.Name}");
            item.Delete();
        }
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
                dw.WriteLine("Ka 1.000000 1.000000 1.000000");
                dw.WriteLine("Kd 1.000000 1.000000 1.000000");
                dw.WriteLine("Ks 0.000000 0.000000 0.000000");
                dw.WriteLine($"map_Kd {texture}.png");
            }
        }

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "obj")))
        {
            dw.WriteLine($"mtllib {Path.GetFileNameWithoutExtension(item.FullName)}.MTL");

            int m = 0;
            int v = 1;
            int vt = 0;

            foreach (var mesh in wmf.Meshes)
            {
                dw.WriteLine($"g geometry_{m}");

                foreach (var point in mesh.Points)
                {
                    dw.WriteLine($"v {point.X * settings.ScaleFactor} {-point.Y * settings.ScaleFactor} {-point.Z * settings.ScaleFactor} 1");
                }

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
                    dw.WriteLine($"vt {face.UV2.U / width} {1f - face.UV2.V / height}");
                    dw.WriteLine($"vt {face.UV3.U / width} {1f - face.UV3.V / height}");
                    dw.WriteLine($"vt {face.UV4.U / width} {1f - face.UV4.V / height}");

                    dw.WriteLine($"f {v + face.V1}/{vt + 1} {v + face.V2}/{vt + 2} {v + face.V3}/{vt + 3}");
                    dw.WriteLine($"f {v + face.V4}/{vt + 4} {v + face.V3}/{vt + 3} {v + face.V2}/{vt + 2}");

                    vt += 4;
                }

                v += mesh.Points.Count;

                m++;
            }
        }

        if (!settings.KeepProcessedFiles)
        {
            Console.WriteLine($"Deleting {item.Name}");
            item.Delete();
        }
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
                dw.WriteLine("Ka 1.000000 1.000000 1.000000");
                dw.WriteLine("Kd 1.000000 1.000000 1.000000");
                dw.WriteLine("Ks 0.000000 0.000000 0.000000");
                dw.WriteLine(@$"map_Kd TEXTURES\{texture}.png");
            }
        }

        using (TextWriter dw = new StreamWriter(Path.ChangeExtension(item.FullName, "obj")))
        {
            dw.WriteLine($"mtllib {Path.GetFileNameWithoutExtension(item.FullName)}.MTL");

            int m = 0;
            int v = 1;
            int b = 0;

            foreach (var entry in wwf.UVTable)
            {
                if (entry.MaterialIndex == 0) { break; }

                string textureName = wwf.Textures[entry.MaterialIndex - 1];
                float width = 127f;
                float height = 127f;

                if (imageLookup.TryGetValue(textureName, out dynamic? value))
                {
                    width = value.X - 1f;
                    height = value.Y - 1f;
                }

                dw.WriteLine($"vt {entry.UV1.U / width} {1f - entry.UV1.V / height}");
                dw.WriteLine($"vt {entry.UV2.U / width} {1f - entry.UV2.V / height}");
                dw.WriteLine($"vt {entry.UV3.U / width} {1f - entry.UV3.V / height}");
                dw.WriteLine($"vt {entry.UV4.U / width} {1f - entry.UV4.V / height}");
            }

            foreach (var block in wwf.MapBlocks)
            {
                m = 0;
                float tX = 0;
                float tY = 0;
                float tZ = 0;
                string lastMaterial = null;

                foreach (var mesh in block.Meshes)
                {
                    dw.WriteLine($"g BLOCK_{block.X}x{block.Y}_{m}");

                    tX = mesh.Offset.X * 256;
                    tY = -mesh.Offset.Y * 256;
                    tZ = -mesh.Offset.Z * 256;

                    tX += block.X * 16384;
                    tY += -block.Y * 16384;

                    tX *= settings.ScaleFactor;
                    tY *= settings.ScaleFactor;
                    tZ *= settings.ScaleFactor;

                    foreach (var point in mesh.Points)
                    {
                        dw.WriteLine($"v {point.X * settings.ScaleFactor + tX} {-point.Y * settings.ScaleFactor + tY} {-point.Z * settings.ScaleFactor + tZ} 1");
                    }

                    foreach (var face in mesh.Faces)
                    {
                        string materialName = wwf.Textures[wwf.UVTable[face.UVIndex].MaterialIndex - 1];
                        if (lastMaterial != materialName)
                        {
                            dw.WriteLine($"usemtl {materialName}");
                            lastMaterial = materialName;
                        }

                        dw.WriteLine($"f {v + face.V4}/{face.UVIndex * 4 + 3} {v + face.V2}/{face.UVIndex * 4 + 2} {v + face.V1}/{face.UVIndex * 4 + 1}");
                        dw.WriteLine($"f {v + face.V3}/{face.UVIndex * 4 + 4} {v + face.V4}/{face.UVIndex * 4 + 3} {v + face.V1}/{face.UVIndex * 4 + 1}");
                    }

                    v += mesh.Points.Count;

                    m++;
                }

                b++;
            }

            b = 0;

            foreach (var section in wwf.Sections)
            {
                dw.WriteLine($"g TRACKDAT_{b}");

                m = 0;

                foreach (var thing in section.Things)
                {
                    dw.WriteLine($"v {(section.OffsetX + section.OffsetXMultiplier * 65535 + thing.P.X) * settings.ScaleFactor} {(-section.OffsetY + section.OffsetYMultiplier * -65535 + -thing.P.Y) * settings.ScaleFactor} {-thing.P.Z * settings.ScaleFactor} 1");
                }

                for (int c = 0; c < section.Columns - 1; c++)
                {
                    for (int r = 0; r < section.Rows - 1; r++)
                    {
                        ushort uvIndex = section.Things[(c + 0) * section.Rows + r + 0].UVIndex;
                        var entry = wwf.UVTable[uvIndex];
                        string textureName = wwf.Textures[entry.MaterialIndex - 1];

                        dw.WriteLine($"usemtl {textureName}");

                        dw.WriteLine($"f {v + (c + 0) * section.Rows + r + 0}/{uvIndex * 4 + 1} {v + (c + 1) * section.Rows + r + 0}/{uvIndex * 4 + 2} {v + (c + 1) * section.Rows + r + 1}/{uvIndex * 4 + 4}");
                        dw.WriteLine($"f {v + (c + 0) * section.Rows + r + 0}/{uvIndex * 4 + 1} {v + (c + 1) * section.Rows + r + 1}/{uvIndex * 4 + 4} {v + (c + 0) * section.Rows + r + 1}/{uvIndex * 4 + 3}");
                    }
                }

                v += section.Things.Count;

                b++;
            }
        }

        if (!settings.KeepProcessedFiles)
        {
            Console.WriteLine($"Deleting {item.Name}");
            item.Delete();
        }
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
            int v = 1;
            int vt = 0;

            foreach (var mesh in s33.Meshes)
            {
                dw.WriteLine($"g geometry_{m}");

                foreach (var point in mesh.Points)
                {
                    dw.WriteLine($"v {point.X * settings.ScaleFactor} {-point.Y * settings.ScaleFactor} {-point.Z * settings.ScaleFactor} 1");
                }

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

                    dw.WriteLine($"usemtl {s33.Textures[face.MaterialIndex]}");

                    dw.WriteLine($"vt {face.UV1.U / width} {1f - face.UV1.V / height}");
                    dw.WriteLine($"vt {face.UV2.U / width} {1f - face.UV2.V / height}");
                    dw.WriteLine($"vt {face.UV3.U / width} {1f - face.UV3.V / height}");
                    dw.WriteLine($"vt {face.UV4.U / width} {1f - face.UV4.V / height}");

                    dw.WriteLine($"f {v + face.V1}/{vt + 1} {v + face.V2}/{vt + 2} {v + face.V3}/{vt + 3}");
                    dw.WriteLine($"f {v + face.V4}/{vt + 4} {v + face.V3}/{vt + 3} {v + face.V2}/{vt + 2}");

                    vt += 4;
                }

                v += mesh.Points.Count;

                m++;
            }
        }

        if (!settings.KeepProcessedFiles)
        {
            Console.WriteLine($"Deleting {item.Name}");
            item.Delete();
        }
    }

    if (!keep && !settings.KeepProcessedFiles)
    {
        File.Delete(Path.ChangeExtension(path, "dat"));
        File.Delete(path);
    }
}