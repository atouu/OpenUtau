using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenUtau.Core.Render;
using Serilog;

namespace OpenUtau.Core.Util {

    public static class Preferences {
        public static SerializablePreferences Default { get; private set; }

        static Preferences() {
            Load();
        }

        public static void Save() {
            try {
                File.WriteAllText(PathManager.Inst.PrefsFilePath,
                    JsonSerializer.Serialize(Default, PrefsJsonContext.Default.SerializablePreferences),
                    Encoding.UTF8);
            } catch (Exception e) {
                Log.Error(e, "Failed to save prefs.");
            }
        }

        public static void Reset() {
            Default = new SerializablePreferences();
            try
            {
                string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string shippedPrefsPath = Path.Combine(exePath, "prefs-default.json");
                if (File.Exists(shippedPrefsPath)) {
                    var shippedPrefs = JsonSerializer.Deserialize<SerializablePreferences>(
                        File.ReadAllText(shippedPrefsPath, Encoding.UTF8),
                        PrefsJsonContext.Default.SerializablePreferences);
                    if (shippedPrefs != null) {
                        Default = shippedPrefs;
                    }
                }
            } catch(Exception e){
                Log.Error(e, "failed to load prefs-default.json");
            }
            Save();
        }

        public static List<string> GetSingerSearchPaths() {
            return new List<string>(Default.SingerSearchPaths);
        }

        public static void SetSingerSearchPaths(List<string> paths) {
            Default.SingerSearchPaths = new List<string>(paths);
            Save();
        }

        public static void AddRecentFileIfEnabled(string filePath){
            //Users can choose adding .ust, .vsqx and .mid files to recent files or not
            string ext = Path.GetExtension(filePath);
            switch(ext){
                case ".ustx":
                    AddRecentFile(filePath);
                    break;
                case ".mid":
                case ".midi":
                    if(Preferences.Default.RememberMid){
                        AddRecentFile(filePath);
                    }
                    break;
                case ".ust":
                    if(Preferences.Default.RememberUst){
                        AddRecentFile(filePath);
                    }
                    break;
                case ".vsqx":
                    if(Preferences.Default.RememberVsqx){
                        AddRecentFile(filePath);
                    }
                    break;
                default:
                    break;
            }
        }

        private static void AddRecentFile(string filePath) {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                return;
            }
            var recent = Default.RecentFiles;
            recent.RemoveAll(f => f == filePath);
            recent.Insert(0, filePath);
            recent.RemoveAll(f => string.IsNullOrEmpty(f)
                || !File.Exists(f)
                || f.Contains(PathManager.Inst.TemplatesPath));
            if (recent.Count > 16) {
                recent.RemoveRange(16, recent.Count - 16);
            }
            Save();
        }

        private static void Load() {
            try {
                if (File.Exists(PathManager.Inst.PrefsFilePath)) {
                    Default = JsonSerializer.Deserialize<SerializablePreferences>(
                        File.ReadAllText(PathManager.Inst.PrefsFilePath, Encoding.UTF8),
                        PrefsJsonContext.Default.SerializablePreferences);
                    if(Default == null) {
                        Reset();
                        return;
                    }

                    if (!ValidString(new Action(() => CultureInfo.GetCultureInfo(Default.Language)))) Default.Language = string.Empty;
                    if (!ValidString(new Action(() => CultureInfo.GetCultureInfo(Default.SortingOrder)))) Default.SortingOrder = string.Empty;
                    if (!Renderers.getRendererOptions().Contains(Default.DefaultRenderer)) Default.DefaultRenderer = string.Empty;
                    if (!Onnx.getRunnerOptions().Contains(Default.OnnxRunner)) Default.OnnxRunner = string.Empty;
                    if (Default.Theme != null) {
                        Default.ThemeName = Default.Theme switch {
                            1 => "Dark",
                            _ => "Light"
                        };
                        Default.Theme = null;
                    }
                } else {
                    Reset();
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load prefs.");
                Default = new SerializablePreferences();
            }
        }

        private static bool ValidString(Action action) {
            try {
                action();
                return true;
            } catch {
                return false;
            }
        }

        [Serializable]
        public partial class SerializablePreferences {
            public WindowSize MainWindowSize { get; set; } = new WindowSize();
            public WindowSize PianorollWindowSize { get; set; } = new WindowSize();
            public int UndoLimit { get; set; } = 100;
            public List<string> SingerSearchPaths { get; set; } = new List<string>();
            public string PlaybackDevice { get; set; } = string.Empty;
            public int PlaybackDeviceNumber {get; set; }
            public int? PlaybackDeviceIndex {get; set; }
            public bool ShowPrefs { get; set; } = true;
            public bool ShowTips { get; set; } = true;
            public string ThemeName { get; set; } = "Light";
            public bool PenPlusDefault { get; set; } = false;
            public int DegreeStyle {get; set; }
            public bool UseTrackColor { get; set; } = false;
            public bool ClearCacheOnQuit { get; set; } = false;
            public bool PreRender { get; set; } = true;
            public int NumRenderThreads { get; set; } = 2;
            public string DefaultRenderer { get; set; } = string.Empty;
            public int WorldlineR { get; set; } = 0;
            public string OnnxRunner { get; set; } = string.Empty;
            public int OnnxGpu { get; set; } = 0;
            public double DiffSingerDepth { get; set; } = 1.0;
            public int DiffSingerSteps { get; set; } = 20;
            public int DiffSingerStepsVariance { get; set; } = 20;
            public int DiffSingerStepsPitch { get; set; } = 10;
            public bool DiffSingerTensorCache { get; set; } = true;
            public bool DiffSingerLangCodeHide { get; set; } = false;
            public bool SkipRenderingMutedTracks { get; set; } = false;
            public string Language { get; set; } = string.Empty;
            public string? SortingOrder { get; set; } = null;
            public List<string> RecentFiles { get; set; } = new List<string>();
            public string SkipUpdate { get; set; } = string.Empty;
            public string AdditionalSingerPath { get; set; } = string.Empty;
            public bool InstallToAdditionalSingersPath { get; set; } = true;
            public bool LoadDeepFolderSinger { get; set; } = true;
            public bool PreferCommaSeparator { get; set; } = false;
            public bool ResamplerLogging { get; set; } = false;
            public List<string> RecentSingers { get; set; } = new List<string>();
            public List<string> FavoriteSingers { get; set; } = new List<string>();
            public Dictionary<string, string> SingerPhonemizers { get; set; } = new Dictionary<string, string>();
            public List<string> RecentPhonemizers { get; set; } = new List<string>();
            public bool PreferPortAudio { get; set; } = false;
            public bool UseSystemDefaultAudioDevice { get; set; } = true;
            public double PlayPosMarkerMargin { get; set; } = 0.9;
            public int LockStartTime { get; set; } = 0;
            public int PlaybackAutoScroll { get; set; } = 2;
            public bool ReverseLogOrder { get; set; } = true;
            public bool ShowPortrait { get; set; } = true;
            public bool ShowIcon { get; set; } = true;
            public bool ShowGhostNotes { get; set; } = true;
            public bool PlayTone { get; set; } = true;
            public bool ShowVibrato { get; set; } = true;
            public bool ShowPitch { get; set; } = true;
            public bool ShowFinalPitch { get; set; } = true;
            public bool ShowWaveform { get; set; } = true;
            public bool ShowPhoneme { get; set; } = true;
            public bool ShowExpressions { get; set; } = true;
            public bool ShowPhonemizerTags { get; set; } = true;
            public bool ShowNoteParams { get; set; } = true;
            public Dictionary<string, string> DefaultResamplers { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, string> DefaultWavtools { get; set; } = new Dictionary<string, string>();
            public string LyricHelper { get; set; } = string.Empty;
            public bool LyricsHelperBrackets { get; set; } = false;
            public int OtoEditor { get; set; } = 0;
            public string VLabelerPath { get; set; } = string.Empty;
            public string SetParamPath { get; set; } = string.Empty;
            public bool Beta { get; set; } = false;
            public bool RememberMid { get; set; } = false;
            public bool RememberUst { get; set; } = true;
            public bool RememberVsqx { get; set; } = true;
            public string WinePath { get; set; } = string.Empty;
            public string PhoneticAssistant { get; set; } = string.Empty;
            public string RecentOpenSingerDirectory { get; set; } = string.Empty;
            public string RecentOpenProjectDirectory { get; set; } = string.Empty;
            public bool LockUnselectedNotesPitch { get; set; } = true;
            public bool LockUnselectedNotesVibrato { get; set; } = true;
            public bool LockUnselectedNotesExpressions { get; set; } = true;
            public bool VoicebankPublishUseIgnore { get; set; } = true;
            public string VoicebankPublishIgnores { get; set; } = @"#Adobe Audition
*.pkf

#UTAU Engines
*.ctspec
*.d4c
*.dio
*.frc
*.frt
#*.frq
*.harvest
*.lessaudio
*.llsm
*.mrq
*.pitchtier
*.pkf
*.platinum
*.pmk
*.sc.npz
*.star
*.uspec
*.vs4ufrq

#UTAU related tools
\$read
*.setParam-Scache
*.lbp
*.lbp.caches/*

#OpenUtau
errors.txt
";
            public string RecoveryPath { get; set; } = string.Empty;
            public bool DetachPianoRoll { get; set; } = false;

            // Legacy
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? Theme {get; set; }
        }

    }

    [JsonSourceGenerationOptions(WriteIndented = true, NewLine = "\n")]
    [JsonSerializable(typeof(Preferences.SerializablePreferences))]
    internal partial class PrefsJsonContext : JsonSerializerContext { }
}
