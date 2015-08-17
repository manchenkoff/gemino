using Sync;
using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Diagnostics;

namespace Gemino.GUI {
    /// <summary>
    /// Interaction logic for Downloader.xaml
    /// </summary>
    public partial class Downloader : Window {
        public Downloader() {
            InitializeComponent();
            Init();
        }

        private void Init() {
            if (Updater.UpdatesAvailable) {

                string file = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "Gemino Setup.exe"
                );

                using (WebClient downloader = new WebClient()) {
                    downloader.DownloadFileAsync(new Uri(Updater.SetupURI), file);
                    downloader.DownloadProgressChanged += (s, e) => {
                        downloadProgress.Value = e.ProgressPercentage;
                    };
                    downloader.DownloadFileCompleted += (s, e) => {
                        Process setup = new Process {
                            StartInfo = new ProcessStartInfo(file)
                        };

                        try {
                            setup.Start();
                            App.Current.Shutdown(2);
                        } catch (Exception ex) {
                            System.Windows.Forms.MessageBox.Show(
                                ex.Message,
                                "Gemino Updater - Error",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error
                                );
                        }
                    };
                }
            }
        }
    }
}
