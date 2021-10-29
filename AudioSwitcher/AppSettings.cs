using AudioSwitcher.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AudioSwitcher
{
    public class AppSettings<T> where T : new()
    {
        private const string DEFAULT_FILENAME = "Settings.json";

        public static void Save(string fileName = DEFAULT_FILENAME)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(new LocalSettings()
            {
                Device1Name = Settings.Device1Name,
                Device2Name = Settings.Device2Name,
                Device1Information = Settings.Device1Information,
                Device2Information = Settings.Device2Information,
                Hotkeys = Settings.Hotkeys.Keys.ToList(),
            }));
        }

        public static void Save(T pSettings, string fileName = DEFAULT_FILENAME)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(pSettings));
        }

        public static LocalSettings Load(string fileName = DEFAULT_FILENAME)
        {
            LocalSettings t = new LocalSettings();
            if (File.Exists(fileName))
                t = JsonConvert.DeserializeObject<LocalSettings>(File.ReadAllText(fileName));
            if (t != null)
            {
                Settings.Device1Name = t.Device1Name ?? Settings.Device1Name;
                Settings.Device2Name = t.Device2Name ?? Settings.Device2Name;
                Settings.Device1Information = t.Device1Information ?? Settings.Device1Information;
                Settings.Device2Information = t.Device2Information ?? Settings.Device2Information;
                Settings.Hotkeys = t.Hotkeys?.ToDictionary(key => key, pressed => false) ?? Settings.Hotkeys;
            }
            return t;
        }
    }
    public class Settings : AppSettings<Settings>
    {
        public static string Device1Name { get; set; } = "Speakers";
        public static string Device1Information { get; set; } = "Focusrite Usb Audio";
        public static string Device2Name { get; set; } = "Speakers";
        public static string Device2Information { get; set; } = "Realtek High Definition Audio";
        public static Dictionary<VirtualCode, bool> Hotkeys { get; set; } = new Dictionary<VirtualCode, bool>() { { VirtualCode.LCONTROL, false }, { VirtualCode.LMENU, false }, { VirtualCode.KEY_M, false } };
    }

    public class LocalSettings
    {
        public string Device1Name { get; set; }
        public string Device1Information { get; set; }
        public string Device2Name { get; set; }
        public string Device2Information { get; set; }
        public List<VirtualCode> Hotkeys { get; set; }
    }
}