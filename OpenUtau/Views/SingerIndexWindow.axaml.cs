using System;
using Avalonia.ReactiveUI;
using OpenUtau.App.ViewModels;
using ReactiveUI;

namespace OpenUtau.App.Views {
    public partial class SingerIndexWindow : ReactiveWindow<SingerIndexViewModel> {

        public SingerIndexWindow() {
            DataContext = ViewModel = new SingerIndexViewModel();
            InitializeComponent();

            this.WhenActivated(d => {
                d(ViewModel
                    .DownloadVariant
                    .RegisterHandler(async interaction => {
                        IsEnabled = false;
                        var variant = interaction.Input;
                        bool isDirectDownload = await variant.IsValidDirectDownload;
                        bool output = false;

                        if (isDirectDownload) {
                            var result = await MessageBox.Show(
                                this,
                                $"Are you sure you want to install '{variant.Names["en"]}'?",
                                "Singer Install",
                                MessageBox.MessageBoxButtons.YesNo);
                            output = result == MessageBox.MessageBoxResult.Yes;
                        } else {
                            var result = await MessageBox.Show(
                                this,
                                $"Invalid direct download for variant '{variant.Id}'. Open in browser instead?",
                                "Invalid Direct Download",
                                MessageBox.MessageBoxButtons.YesNo);
                            if (result == MessageBox.MessageBoxResult.Yes) {
                                await Launcher.LaunchUriAsync(new Uri(variant.FileUrl));
                            }
                        }

                        interaction.SetOutput(output);
                        IsEnabled = true;
                    }));
            });
        }
    }
}