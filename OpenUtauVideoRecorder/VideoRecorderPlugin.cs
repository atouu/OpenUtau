using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using OpenUtauVideoRecorder.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using OpenUtau.App.Views;
using OpenUtauVideoRecorder.ViewModels;

namespace OpenUtauVideoRecorder {
    public class VideoRecorderPlugin : BatchEdit {
        public string Name => "Video Recorder";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) {
                if (lifetime.MainWindow is MainWindow mw) {
                    var recorder = new RecorderDialog(mw);
                    recorder.Show();
                } else return;
            } else return;
        }

    }
}