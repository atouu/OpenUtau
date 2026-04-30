using System.Reactive.Linq;
using ReactiveUI;

namespace OpenUtauVideoRecorder.ViewModels {

    public class RecorderViewModel : ReactiveObject {
        private string _ffMpegPath;
        public string FFMpegPath {
            get => _ffMpegPath;
            set => this.RaiseAndSetIfChanged(ref _ffMpegPath, value);
        }

        private string _ffMpegArgs;
        public string FFMpegArgs {
            get => _ffMpegArgs;
            set => this.RaiseAndSetIfChanged(ref _ffMpegArgs, value);
        }

        private string _outputPath;
        public string OutputPath {
            get => _outputPath;
            set => this.RaiseAndSetIfChanged(ref _outputPath, value);
        }

        private int _videoFPS;
        public int VideoFPS {
            get => _videoFPS;
            set => this.RaiseAndSetIfChanged(ref _videoFPS, value);
        }

        private int _videoHeight;
        public int VideoHeight {
            get => _videoHeight;
            set => this.RaiseAndSetIfChanged(ref _videoHeight, value);
        }

        private int _videoWidth;
        public int VideoWidth {
            get => _videoWidth;
            set => this.RaiseAndSetIfChanged(ref _videoWidth, value);
        }

        private bool _isRecording;
        public bool IsRecording {
            get => _isRecording;
            set => this.RaiseAndSetIfChanged(ref _isRecording, value);
        }

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