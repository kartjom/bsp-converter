using System;
using System.IO;
using System.Text.Json;

namespace Decompiler
{
    public class ConfigData
    {
        public string CoD1RootDirectory { get; init; }
        public string CoD2RootDirectory { get; init; }
        public string MoHAARootDirectory { get; init; }
        public bool ShouldSplitObjects { get; set; }
    }

    public static class AppConfig
    {
        public static string Location = Path.Combine(Project.RootLocation, "config.json");

        public static void Create()
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var configData = new ConfigData()
            {
                ShouldSplitObjects = false
            };

            string jsonString = JsonSerializer.Serialize(configData, options).Replace("null", "\"game_path_here\"");

            File.WriteAllText(AppConfig.Location, jsonString);
        }

        public static ConfigData Settings()
        {
            try {
                string jsonFileContent = File.ReadAllText(AppConfig.Location).Replace(@"\", @"/");
                return JsonSerializer.Deserialize<ConfigData>(jsonFileContent);
            }
            catch(FileNotFoundException) {
                Console.WriteLine("INFO: config.json created. Set game path to export textures.");

                AppConfig.Create();
                return new ConfigData();
            }
        }
    }
}
