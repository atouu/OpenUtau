using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtauVideoRecorder.ViewModels;

namespace OpenUtauVideoRecorder.Views {
    public partial class RecorderDialog : Window {

        private RecorderViewModel vm;
        private MainWindow mainWindow;
        private Grid windowGrid;
        private Grid mainGrid;
        private Border border;
        private Viewbox viewbox;
        private CancellationTokenSource cts;

        public RecorderDialog(MainWindow wm) {
            mainWindow = wm;
            DataContext = vm = new RecorderViewModel();
            AvaloniaXamlLoader.Load(this);

            windowGrid = mainWindow.Content as Grid;
            mainGrid = windowGrid?.Children[0] as Grid;
            border = new Border() {
                Background = new SolidColorBrush(Color.Parse("Black")),
                Child = viewbox = new Viewbox() {
                    Stretch = Stretch.Uniform
                }
            };

            if (windowGrid != null && mainGrid != null) {
                windowGrid.Children.Add(border);
                windowGrid.Children.Remove(mainGrid);

                mainGrid.Background = mainWindow.Background;
                mainGrid.Width = vm.VideoWidth;
                mainGrid.Height = vm.VideoHeight;
                viewbox.Child = mainGrid;
            }
        }

        public void WindowClosing(object? sender, WindowClosingEventArgs e) {
            mainGrid.Background = null;
            mainGrid.Width = double.NaN;
            mainGrid.Height = double.NaN;
            viewbox.Child = null;
            windowGrid.Children.Remove(border);
            windowGrid.Children.Add(mainGrid);
        }

        public async void OnRecord(object sender, RoutedEventArgs args) {
            if (!Path.Exists(vm.FFMpegPath)) {
                MessageBox.ShowModal(this, $"FFmpeg path \"{vm.FFMpegPath}\" not found.", Title);
                return;
            }
            if (string.IsNullOrEmpty(vm.OutputPath)) {
                MessageBox.ShowModal(this, "Output path cannot be empty.", Title);
                return;
            }

            cts = new CancellationTokenSource();

            vm.IsRecording = false;

            try {
                await Record();
            } catch (OperationCanceledException) {
            } catch (Exception e) {
                await MessageBox.ShowError(this, e);
            };
            vm.IsRecording = false;
        }

        private async Task Record() {
            var pxSize = new PixelSize(vm.VideoWidth, vm.VideoHeight);

            var audiopath = Path.Join(PathManager.Inst.CachePath, $"{Guid.NewGuid()}.wav");

            await PlaybackManager.Inst.RenderMixdown(DocManager.Inst.Project, audiopath);

            await Dispatcher.UIThread.InvokeAsync(() => DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Empty)));

            var processInfo = new ProcessStartInfo {
                FileName = vm.FFMpegPath,
                Arguments = $"-y -f rawvideo -framerate {vm.VideoFPS} -pix_fmt bgra -s {pxSize.Width}x{pxSize.Height} -i pipe:0 -i {audiopath} {vm.FFMpegArgs} {vm.OutputPath}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processInfo;

            process.Start();

            using var inputStream = process.StandardInput.BaseStream;

            int bytesPerPixel = 4;
            int stride = pxSize.Width * bytesPerPixel;

            byte[] buffer = new byte[stride * pxSize.Height];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var ptr = handle.AddrOfPinnedObject();

            using var cb = new RenderTargetBitmap(pxSize);
            int incr = DocManager.Inst.Project.timeAxis.MsPosToTickPos(1000) / vm.VideoFPS;

            for (int i = 0; i < DocManager.Inst.Project.EndTick; i += incr) {
                if (cts.IsCancellationRequested) {
                    break;
                }
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(i));
                cb.Render(mainGrid);
                cb.CopyPixels(new PixelRect(0, 0, pxSize.Width, pxSize.Height), ptr, buffer.Length, stride);
                await inputStream.WriteAsync(buffer, cts.Token);
            }

            process.Close();

            File.Delete(audiopath);

            if (handle.IsAllocated)
                handle.Free();

            inputStream.Dispose();

            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Video Render Done"));
        }

        public void OnStop(object sender, RoutedEventArgs args) {
            cts.Cancel();
        }

        private void OnVideoHeightChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (e.NewValue == null) {
                ((NumericUpDown) sender).Value = 0;
                return;
            }
            mainGrid.Height = (double) e.NewValue.Value;
        }

        private void OnVideoWidthChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (e.NewValue == null) {
                ((NumericUpDown) sender).Value = 0;
                return;
            }
            mainGrid.Width = (double) e.NewValue.Value;
        }

        private void OnVideoFPSChanged(object sender, NumericUpDownValueChangedEventArgs e) {
            if (e.NewValue == null) {
                ((NumericUpDown) sender).Value = 1;
                return;
            }
        }

        private async void OnSelectFFMpegPath(object sender, RoutedEventArgs e) {
            var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = "Select FFMpeg Executable",
                AllowMultiple = false,
                FileTypeFilter = [ new("Executable") {
                    MimeTypes = ["application/x-executable", "application/vnd.microsoft.portable-executable" ],
                    AppleUniformTypeIdentifiers = [ "public.unix-executable" ]
                }]
            });
            if (files != null) {
                vm.FFMpegPath = files[0].Path.AbsolutePath;
            }
        }

        private async void OnSelectOutputPath(object sender, RoutedEventArgs e) {
            var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "Save Video As",
                DefaultExtension = "mp4",
                SuggestedFileName = "output"
            });
            if (file != null) {
                vm.OutputPath = file.Path.AbsolutePath;
            }
        }
    }
}
