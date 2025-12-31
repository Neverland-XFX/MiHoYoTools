using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Windows.Storage;

namespace MiHoYoTools.Core
{
    public static class AppLocalSettings
    {
        private static readonly object SyncRoot = new object();
        private static readonly string SettingsPath = Path.Combine(AppPaths.Root, "LocalSettings.json");
        private static bool _useFileStore;
        private static Dictionary<string, string> _fileStore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static AppLocalSettings()
        {
            try
            {
                var _ = ApplicationData.Current.LocalSettings;
                _useFileStore = false;
            }
            catch
            {
                _useFileStore = true;
                LoadFileStore();
            }
        }

        public static bool IsPackaged => !_useFileStore;

        public static bool ContainsKey(string key)
        {
            if (!_useFileStore)
            {
                return ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
            }

            lock (SyncRoot)
            {
                return _fileStore.ContainsKey(key);
            }
        }

        public static T GetValue<T>(string key, T defaultValue = default)
        {
            if (!_useFileStore)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                return localSettings.Values.ContainsKey(key) ? (T)localSettings.Values[key] : defaultValue;
            }

            lock (SyncRoot)
            {
                if (!_fileStore.TryGetValue(key, out var value))
                {
                    return defaultValue;
                }
                return ConvertFromString(value, defaultValue);
            }
        }

        public static void SetValue<T>(string key, T value)
        {
            if (!_useFileStore)
            {
                ApplicationData.Current.LocalSettings.Values[key] = value;
                return;
            }

            lock (SyncRoot)
            {
                _fileStore[key] = ConvertToString(value);
                SaveFileStore();
            }
        }

        public static void RemoveValue(string key)
        {
            if (!_useFileStore)
            {
                ApplicationData.Current.LocalSettings.Values.Remove(key);
                return;
            }

            lock (SyncRoot)
            {
                if (_fileStore.Remove(key))
                {
                    SaveFileStore();
                }
            }
        }

        public static void Clear()
        {
            if (!_useFileStore)
            {
                ApplicationData.Current.LocalSettings.Values.Clear();
                return;
            }

            lock (SyncRoot)
            {
                _fileStore.Clear();
                SaveFileStore();
            }
        }

        private static void LoadFileStore()
        {
            Directory.CreateDirectory(AppPaths.Root);
            if (!File.Exists(SettingsPath))
            {
                _fileStore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            try
            {
                var json = File.ReadAllText(SettingsPath);
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                _fileStore = data != null
                    ? new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                _fileStore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void SaveFileStore()
        {
            Directory.CreateDirectory(AppPaths.Root);
            var json = JsonConvert.SerializeObject(_fileStore, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }

        private static string ConvertToString<T>(T value)
        {
            return value?.ToString() ?? string.Empty;
        }

        private static T ConvertFromString<T>(string value, T defaultValue)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
