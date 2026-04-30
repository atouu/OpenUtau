using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtauVideoRecorder.ViewModels {

    public class RecorderViewModel : ReactiveObject {
        [Reactive] public string FFMpegPath { get; set; }
        [Reactive] public string FFMpegArgs { get; set; }
        [Reactive] public string OutputPath { get; set; }
        [Reactive] public int VideoFPS { get; set; }
        [Reactive] public int VideoHeight { get; set; }
        [Reactive] public int VideoWidth { get; set; }
        [Reactive] public bool IsRecording { get; set; }

        public RecorderViewModel() {
            FFMpegPath = Preferences.Default.FFMpegPath;
            FFMpegArgs = Preferences.Default.FFMpegArgs;
            VideoFPS = Preferences.Default.VideoFPS;
            VideoHeight = Preferences.Default.VideoHeight;
            VideoWidth = Preferences.Default.VideoWidth;

            this.WhenAnyValue(vm => vm.FFMpegPath)
                .Subscribe(v => {
                    Preferences.Default.FFMpegPath = v;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.FFMpegArgs)
                .Subscribe(v => {
                    Preferences.Default.FFMpegArgs = v;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.VideoFPS)
                .Subscribe(v => {
                    Preferences.Default.VideoFPS = v;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.VideoHeight)
                .Subscribe(v => {
                    Preferences.Default.VideoHeight = v;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.VideoWidth)
                .Subscribe(v => {
                    Preferences.Default.VideoWidth = v;
                    Preferences.Save();
                });
        }
    }
}