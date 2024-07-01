using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CodeAggregatorGtk
{
    public class SettingsHandler
    {
        private const string SettingsFile = "settings.json";
        public Settings Settings { get; set; }

        public SettingsHandler()
        {
            Settings = new Settings();
            LoadSettings();
        }

        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                Settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
            }
            else
            {
                Settings = new Settings();
            }
        }
    }
}
