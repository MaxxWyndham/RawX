using System.Text.Json;

namespace RawX
{
    public class Settings
    {
        public bool ForcePowerOfTwo { get; set; }

        public bool KeepProcessedFiles { get; set; }

        public float ScaleFactor { get; set; } = 1.0f;

        public static Settings Load(string path)
        {
            if (Path.Exists(path)) { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)); }

            return new Settings();
        }

        public void Save(string path)
        {
            using FileStream stream = File.Create(path);
            JsonSerializer.Serialize(stream, this);
        }
    }
}
