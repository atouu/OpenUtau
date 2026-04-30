using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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

        public async void Record(object sender, RoutedEventArgs args) {
            vm.IsRecording = true;

            var pxSize = new PixelSize(vm.VideoWidth, vm.VideoHeight);

            var audiopath = Path.ChangeExtension(vm.OutputPath, "wav");

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
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(i));
                cb.Render(mainGrid);
                cb.CopyPixels(new PixelRect(0, 0, pxSize.Width, pxSize.Height), ptr, buffer.Length, stride);
                await inputStream.WriteAsync(buffer);
            }

            process.Close();

            if (handle.IsAllocated)
                handle.Free();

            inputStream.Dispose();

            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Video Render Done"));
            vm.IsRecording = false;
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
    }
}
