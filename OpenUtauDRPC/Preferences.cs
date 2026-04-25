using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtauDRPC {

    public static class Preferences {
        public static SerializablePreferences Default;
        public static readonly string preferencePath =
            Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "discord-prefs.json");

        static Preferences() {
            Load();
        }

        public static void Save() {
            try {
                File.WriteAllText(preferencePath,
                    JsonConvert.SerializeObject(Default, Formatting.Indented),
                    Encoding.UTF8);
            } catch (Exception e) {
                Log.Error(e, "Failed to save prefs.");
            }
        }

        public static void Reset() {
            Default = new SerializablePreferences();
            Save();
        }

        private static void Load() {
            try {
                if (File.Exists(preferencePath)) {
                    Default = JsonConvert.DeserializeObject<SerializablePreferences>(
                        File.ReadAllText(preferencePath, Encoding.UTF8));
                    if(Default == null) {
                        Reset();
                        return;
                    }
                } else {
                    Reset();
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load prefs.");
                Default = new SerializablePreferences();
            }
        }

        [Serializable]
        public class SerializablePreferences {
            public string ApplicationId = "1462124130966048822";
            public Dictionary<string, string> SingerIconUrls = new() {
                {"Pumpking the Testloid", "https://static.vocadb.net/img/Artist/additionalOrig/1758.jpg"}
            };
        }
    }
}
