using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Chizl.Applications
{
    internal static class ConfigData
    {
        private const int _configNameSize = 20;
        private static string FilePath = @".\config.dat";

        /// <summary>
        /// Add or Update existing config value by config name.
        /// </summary>
        /// <typeparam name="T">Object type of config value</typeparam>
        /// <param name="configName">Config Name</param>
        /// <param name="configValue">Config Value</param>
        /// <param name="autoSave">If the value was added or updated success, auto save to file.</param>
        /// <returns>true if successfully added or updated</returns>
        public static bool AddItem<T>(string configName, T configValue, bool autoSave = false)
        {
            var retVal = false;

            if (ConfigLabels.TryGetValue(configName, out object nConfigValue))
                retVal = ConfigLabels.TryUpdate(configName, configValue, nConfigValue);
            else
                retVal = ConfigLabels.TryAdd(configName, configValue);

            if (retVal && autoSave) SaveConfig();

            return retVal;
        }

        /// <summary>
        /// Get config value by config name.
        /// </summary>
        /// <typeparam name="T">Object type of config value</typeparam>
        /// <param name="configName">Config Name</param>
        /// <param name="configValue">Config Value</param>
        /// <returns>true if successfully found and convert to typeof(T)</returns>
        public static bool GetItem<T>(string configName, T defaultValue, out T configValue)
        {
            var t = typeof(T);
            configValue = defaultValue;

            if (ConfigLabels.TryGetValue(configName, out object foundValue))
            {
                foundValue = foundValue.ToString().Trim();
                try
                {
                    switch (typeof(T).Name)
                    {
                        case "Point":
                            var pnt = foundValue.ToString().Replace("{", "").Replace("}", "").Replace(" ", "").Split(',');
                            int x = 0, y = 0;
                            if (pnt.Length == 2)
                            {
                                int.TryParse(pnt[0].Replace($"X=", ""), out x);
                                int.TryParse(pnt[1].Replace($"Y=", ""), out y);
                            }
                            foundValue = new Point(x, y);
                            break;
                        case "Size":
                            var sz = foundValue.ToString().Replace("{", "").Replace("}", "").Replace(" ", "").Split(',');
                            int w = 0, h = 0;
                            if (sz.Length == 2)
                            {
                                int.TryParse(sz[0].Replace($"Width=", ""), out w);
                                int.TryParse(sz[1].Replace($"Height=", ""), out h);
                            }
                            foundValue = new Size(w, h);
                            break;
                        default:
                            break;
                    }

                    configValue = (T)Convert.ChangeType(foundValue, typeof(T));

                    return true;
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Remove Config Value by Config Name
        /// </summary>
        /// <param name="configName">Config Name</param>
        /// <returns>true if successfully found and removed</returns>
        public static bool RemoveItem(string configName) => ConfigLabels.TryRemove(configName, out _);

        /// <summary>
        /// Load file into Dictionary of config information.
        /// </summary>
        /// <param name="filePath">Path of the file.</param>
        public static void LoadConfig(string filePath)
        {
            FilePath = filePath;
            LoadConfig();
        }

        /// <summary>
        /// Save config, only called after AddItem()..
        /// </summary>
        private static void SaveConfig()
        {
            var configData = new List<string>();
            var keys = ConfigLabels.Keys;

            if (keys.Count.Equals(0))
                return;

            foreach (var key in keys)
            {
                var configValue = ConfigLabels[key].ToString().Trim();
                var configName = $"{key}:";

                configData.Add($"{configName}{configValue}");
            }

            if (configData.Count.Equals(0))
                return;

            File.WriteAllLines(FilePath, configData.ToArray());
        }

        /// <summary>
        /// Load file into Dictionary of config information.
        /// </summary>
        private static void LoadConfig()
        {
            if (File.Exists(FilePath))
            {
                foreach (var confLine in File.ReadLines(FilePath).ToList())
                {
                    var sep = confLine.IndexOf(':');
                    if (sep == -1)
                        continue;

                    var configName = confLine.Substring(0, sep).Trim();
                    var configValue = confLine.Substring(sep + 1).Trim();

                    if (!string.IsNullOrWhiteSpace(configName) &&
                        !string.IsNullOrWhiteSpace(configValue))
                        AddItem(configName, configValue);
                }
            }
        }

        /// <summary>
        /// Dictionary that holds all configurations and their values.
        /// </summary>
        private static ConcurrentDictionary<string, object> ConfigLabels { get; } = new ConcurrentDictionary<string, object>();
    }
}
