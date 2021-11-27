using System;
using System.IO;
using Newtonsoft.Json;

namespace PSXPackagerGUI.Common
{
    public class Configuration<T>
    {
        private readonly string _settingsPath;

        public string SettingsPath => _settingsPath;

        public Configuration(string appName, string configName = "config.json")
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _settingsPath = Path.Combine(local, appName, configName);
        }

        public bool TryLoad(out T obj)
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                obj = JsonConvert.DeserializeObject<T>(json);
                return true;
            }

            obj = default(T);

            return false;
        }

        public void Save(T obj)
        {
            var path = Path.GetDirectoryName(_settingsPath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);

            File.WriteAllText(_settingsPath, json);
        }
    }
}