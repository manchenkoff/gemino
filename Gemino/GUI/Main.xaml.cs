using Sync;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace Gemino.GUI {
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window {

        #region Properties
        private Config config;
        private bool IsSaved = true;
        #endregion

        #region Constructors
        public Main() {
            InitializeComponent();
        }
        #endregion

        #region Methods
        void SaveSettings() {
            config.Write();
            Environment.ExitCode = 1;
            IsSaved = true;
        }
        #endregion

        #region Window actions
        private void WindowInit(object sender, EventArgs e) {
            gridSettings.Visibility = Visibility.Hidden;
            
            config = new Config(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Gemino",
                    "settings.ini"
                    )
                );

            DataContext = config;
        }

        private void ShowFolders(object sender, RoutedEventArgs e) {
            gridFolders.Visibility = Visibility.Visible;
            gridSettings.Visibility = Visibility.Hidden;
        }

        private void ShowSettings(object sender, RoutedEventArgs e) {
            gridFolders.Visibility = Visibility.Hidden;
            gridSettings.Visibility = Visibility.Visible;
        }

        private void AddFolder(object sender, RoutedEventArgs e) {
            if (new FolderEditDialog(config).ShowDialog() == true) {
                IsSaved = false;
            }
        }

        private void EditFolder(object sender, RoutedEventArgs e) {
            if (FoldersListView.SelectedItem != null &&
                new FolderEditDialog((SyncObject)FoldersListView.SelectedItem).ShowDialog() == true) {
                IsSaved = false;
            }
        }

        private void RemoveFolder(object sender, RoutedEventArgs e) {
            if (FoldersListView.SelectedItem != null) {
                try {
                    config.Folders.Remove((SyncObject)FoldersListView.SelectedItem);
                    IsSaved = false;
                } catch (Exception ex) {
                    System.Windows.Forms.MessageBox.Show(
                        ex.Message,
                        "Gemino SmartSync - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                        );
                }
            }
        }

        private void AutoloadClick(object sender, RoutedEventArgs e) {
            IsSaved = false;
        }

        private void LogClick(object sender, RoutedEventArgs e) {
            IsSaved = false;
        }

        private void ShowFolderDialog(object sender, RoutedEventArgs e) {
            using (FolderBrowserDialog browseFolderDialog = new FolderBrowserDialog()) {
                browseFolderDialog.Description = "Выберите папку для синхронизаций";
                browseFolderDialog.ShowNewFolderButton = true;
                browseFolderDialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (browseFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    config.SyncPath = browseFolderDialog.SelectedPath;
                    IsSaved = false;
                }
            }
        }

        private void ImportSettings(object sender, RoutedEventArgs e) {
            using (OpenFileDialog browseFileDialog = new OpenFileDialog()) {
                browseFileDialog.Title = "Выберите файл настроек для импорта";
                browseFileDialog.Filter = "INI Settings|*.ini";

                if (browseFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    config.LoadFrom(browseFileDialog.FileName);
                    IsSaved = false;
                }
            }
        }

        private void ExportSettings(object sender, RoutedEventArgs e) {
            using (SaveFileDialog browseFileSaveDialog = new SaveFileDialog()) {
                browseFileSaveDialog.Title = "Выберите файл для экспорта";
                browseFileSaveDialog.Filter = "*.ini|INI Settings";
                browseFileSaveDialog.FileName = "settings.ini";

                if (browseFileSaveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    config.WriteTo(browseFileSaveDialog.FileName);
                }
            }
        }

        private void CheckUpdates(object sender, RoutedEventArgs e) {
            switch (Updater.UpdatesAvailable) {
                case true:
                    UpdatesAvailableText.Text = "Доступна новая версия!";
                    CheckUpdatesButton.Content = "Загрузить обновление";
                    CheckUpdatesButton.Click += (s, es) => {
                        Downloader downloadDialog = new Downloader();
                        downloadDialog.ShowDialog();
                    };
                    break;
                case false:
                    UpdatesAvailableText.Text = "Обновлений нет";
                    break;
            }
        }

        private void Apply(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        private void Save(object sender, RoutedEventArgs e) {
            SaveSettings();
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnFormClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!IsSaved) {
                switch (System.Windows.Forms.MessageBox.Show(
                    "Настройки были изменены! Вы действительно хотите выйти?",
                    "Предупреждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                    )) {
                    case System.Windows.Forms.DialogResult.Yes:
                        e.Cancel = false;
                        break;
                    case System.Windows.Forms.DialogResult.No:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process browser = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo(Updater.SetupURI)
            };
            browser.Start();
        }
        private void ShowAbout(object sender, RoutedEventArgs e) {
            new AboutWindow().ShowDialog();
        }
        #endregion
        
    }
}
