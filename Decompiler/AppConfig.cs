using System;
using System.IO;
using System.Text.Json;
using System.Reflection;

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

            //string rootDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            File.WriteAllText("config.json", jsonString);
        }

        public static ConfigData Settings()
        {
            try {
                string jsonFileContent = File.ReadAllText("config.json").Replace(@"\", @"/");
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
