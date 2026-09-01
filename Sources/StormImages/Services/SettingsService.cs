using System;
using System.IO;
using Newtonsoft.Json;
using StormImages.Models;

namespace StormImages.Services
{
    public class SettingsService
    {
        private static SettingsService? _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private readonly string _configPath;
        public AppSettings Settings { get; private set; }

        private SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "STORM_IMAGES");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            _configPath = Path.Combine(folder, "settings.json");
            Settings = Load();
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch { }

            var def = new AppSettings();
            Save(def);
            return def;
        }

        public void Save(AppSettings? settings = null)
        {
            try
            {
                if (settings != null) Settings = settings;
                string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }
    }
}