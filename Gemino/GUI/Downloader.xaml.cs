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
            Init(); //запускаем инициализацию загрузчика
        }

        /// <summary>
        /// Инициализация загрузчика
        /// </summary>
        private void Init() {
            //если обновления доступны
            if (Updater.UpdatesAvailable) {
                //генерируем путь к файлу в загрузках
                string file = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "Gemino Setup.exe"
                );
                //временно используем веб-клиент для загрузки файла
                using (WebClient downloader = new WebClient()) {
                    //запускаем асинхронную загрузку файла
                    downloader.DownloadFileAsync(new Uri(Updater.SetupURI), file);
                    //меняем полосу загрузки под процент закачки
                    downloader.DownloadProgressChanged += (s, e) => {
                        downloadProgress.Value = e.ProgressPercentage;
                    };
                    //обрабатываем конец загрузки
                    downloader.DownloadFileCompleted += (s, e) => {
                        Process setup = new Process {
                            //запускаем процесс установщика
                            StartInfo = new ProcessStartInfo(file)
                        };

                        try {
                            //запускаем скачанный файл
                            setup.Start();
                            //закрываем приложение с кодом выхода 2
                            App.Current.Shutdown(2);
                        } catch (Exception ex) {
                            //в случае ошибки выводим сообщение
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
