using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtauVideoRecorder {

    public static class Preferences {
        public static SerializablePreferences Default;
        public static readonly string preferencePath =
            Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "video-recorder-prefs.json");

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
            public string FFMpegPath = string.Empty;
            public string FFMpegArgs = "-c:a aac -c:v libx264 -shortest  -filter_complex \"[1:a]apad\" -pix_fmt yuv420p -preset ultrafast";
            public int VideoFPS = 60;
            public int VideoHeight = 1080;
            public int VideoWidth = 1920;
        }
    }
}
