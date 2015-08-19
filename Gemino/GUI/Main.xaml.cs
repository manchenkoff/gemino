using Sync;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace Gemino.GUI {
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window {

        #region Properties
        private Config config; //конфигурация приложения
        private bool _isSaved = true; //сохранены ли изменения

        //свойство для управления состоянием кнопок сохранения
        private bool IsSaved { 
            get { return _isSaved; }
            set {
                _isSaved = value;
                //если файл изменен, тогда включаем кнопки
                ApplyButton.IsEnabled = SaveButton.IsEnabled = !value;
            }
        }
        #endregion

        #region Constructors
        public Main() {
            InitializeComponent();
            CheckProcessStarted(); //проверка на наличиче запущенного Service.exe
            LoadWindowProps(); //загружаем параметры окна из настроек
        }
        #endregion

        #region Methods
        /// <summary>
        /// Проверка запущенного процесса Service.exe
        /// </summary>
        void CheckProcessStarted() {
            //ищем процесс приложения в трее
            Process[] geminoProcs = Process.GetProcessesByName("Service");
            //если уже запущен
            if (geminoProcs != null && geminoProcs.Length > 0) {
                return; //возврат из функции
            } else {
                //иначе создаем процесс Service.exe
                Process startService = new Process {
                    StartInfo = new ProcessStartInfo(
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "Service.exe"
                            )
                        )
                };
                //и запускаем его
                startService.Start();
            }
        }
        /// <summary>
        /// Сохранение настроек
        /// </summary>
        void SaveSettings() {
            config.Write(); //пишем настройки в файл
            Environment.ExitCode = 1; //закрываем программу с кодом выхода 1
            IsSaved = true; //меняем значение булевой переменной
        }

        /// <summary>
        /// Сохраняем свойства окна в настройки программы
        /// </summary>
        void SaveWindowProps() {
            //ширина окна
            Properties.Settings.Default.WindowWidth = (int)Width;
            //высота окна
            Properties.Settings.Default.WindowHeight = (int)Height;
            //состояние, max - на весь экран, normal - обычное
            Properties.Settings.Default.WindowState = (WindowState == WindowState.Maximized) ? "max" : "normal";
            //сохраняем изменения
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Загрузка параметров окна из настроек
        /// </summary>
        void LoadWindowProps() {
            //если состояние - полноэкранное, тогда назначаем его
            if (Properties.Settings.Default.WindowState == "max") {
                WindowState = WindowState.Maximized;
            } else {
                //иначе просто назначаем ширину и высоту окна
                Width = Properties.Settings.Default.WindowWidth;
                Height = Properties.Settings.Default.WindowHeight;
            }
        }
        #endregion

        #region Window actions
        //загрузка окна
        private void WindowInit(object sender, EventArgs e) {
            //отключаем видимость панели настроек программы
            gridSettings.Visibility = Visibility.Hidden;
            //генерируем путь настроек
            config = new Config(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Gemino",
                    "settings.ini"
                    )
                );
            //назначем настройки как контекст окна (для привязки данных)
            DataContext = config;
        }

        //показать панель настроек папок
        private void ShowFolders(object sender, RoutedEventArgs e) {
            gridFolders.Visibility = Visibility.Visible;
            gridSettings.Visibility = Visibility.Hidden;
        }

        //показать панель настроек программы
        private void ShowSettings(object sender, RoutedEventArgs e) {
            gridFolders.Visibility = Visibility.Hidden;
            gridSettings.Visibility = Visibility.Visible;
        }

        //вызов диалога добавления папки
        private void AddFolder(object sender, RoutedEventArgs e) {
            if (new FolderEditDialog(config).ShowDialog() == true) {
                IsSaved = false;
            }
        }

        //вызов диалога редактирования выбранной папки
        private void EditFolder(object sender, RoutedEventArgs e) {
            if (FoldersListView.SelectedItem != null &&
                new FolderEditDialog((SyncObject)FoldersListView.SelectedItem).ShowDialog() == true) {
                IsSaved = false;
            }
        }

        //удаление папки из коллекции
        private void RemoveFolder(object sender, RoutedEventArgs e) {
            if (FoldersListView.SelectedItem != null) {
                try {
                    //попытка удалить папку из коллекции
                    config.Folders.Remove((SyncObject)FoldersListView.SelectedItem);
                    //настройки изменены
                    IsSaved = false;
                } catch (Exception ex) {
                    //в случае исключения выводим сообщение
                    System.Windows.Forms.MessageBox.Show(
                        ex.Message,
                        "Gemino SmartSync - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                        );
                }
            }
        }

        //нажатие на чекбокс "автозагрузка"
        private void AutoloadClick(object sender, RoutedEventArgs e) {
            IsSaved = false;
        }

        //наажатие на чекбокс "вести логи"
        private void LogClick(object sender, RoutedEventArgs e) {
            IsSaved = false;
        }

        //нажатие на "открыть логи"
        private void OpenLogs(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            try {
                //создаем процесс для открытия папки
                System.Diagnostics.Process explorer = new System.Diagnostics.Process {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(
                        //генерируем путь с лог-файлами
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "Gemino", "Logs"
                            )
                        )
                };
                //запускаем проводник
                explorer.Start();
            } catch (Exception ex) {
                //в случае исключения выводим сообщение
                System.Windows.Forms.MessageBox.Show(
                    ex.Message,
                    "Gemino SmartSync - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );
            }
        }

        //выбор папки для синхронизаций по умолчанию
        private void ShowFolderDialog(object sender, RoutedEventArgs e) {
            using (FolderBrowserDialog browseFolderDialog = new FolderBrowserDialog()) {
                browseFolderDialog.Description = "Выберите папку для синхронизаций";
                browseFolderDialog.ShowNewFolderButton = true;
                browseFolderDialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (browseFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    //назначаем путь в настроки
                    config.SyncPath = browseFolderDialog.SelectedPath;
                    IsSaved = false;
                }
            }
        }

        //диалог импорта настроек
        private void ImportSettings(object sender, RoutedEventArgs e) {
            using (OpenFileDialog browseFileDialog = new OpenFileDialog()) {
                browseFileDialog.Title = "Выберите файл настроек для импорта";
                browseFileDialog.Filter = "INI Settings|*.ini";

                if (browseFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    config.LoadFrom(browseFileDialog.FileName); //загружаем из файла настройки
                    IsSaved = false;
                }
            }
        }

        //диалог экспорта настроек
        private void ExportSettings(object sender, RoutedEventArgs e) {
            using (SaveFileDialog browseFileSaveDialog = new SaveFileDialog()) {
                browseFileSaveDialog.Title = "Выберите файл для экспорта";
                browseFileSaveDialog.Filter = "*.ini|INI Settings";
                browseFileSaveDialog.FileName = "settings.ini";

                if (browseFileSaveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    config.WriteTo(browseFileSaveDialog.FileName); //пишем настройки в файл
                }
            }
        }

        //проверка обновлений
        private void CheckUpdates(object sender, RoutedEventArgs e) {
            switch (Updater.UpdatesAvailable) {
                case true:
                    //если доступны, ставим текст рядом с кнопкой
                    UpdatesAvailableText.Text = "Доступна новая версия!";
                    //меняем текст на кнопке проверки
                    CheckUpdatesButton.Content = "Загрузить обновление";
                    //добавляем обработчик нажатия на кнопку
                    CheckUpdatesButton.Click += (s, es) => {
                        //создаем окно загрузчика
                        Downloader downloadDialog = new Downloader();
                        downloadDialog.ShowDialog();
                    };
                    break;
                case false:
                    UpdatesAvailableText.Text = "Обновлений нет";
                    break;
            }
        }

        //кнопка применить
        private void Apply(object sender, RoutedEventArgs e) {
            SaveSettings();
        }

        //кнопка сохранить
        private void Save(object sender, RoutedEventArgs e) {
            SaveSettings();
            Close();
        }

        //кнопка отмены
        private void Cancel(object sender, RoutedEventArgs e) {
            Close();
        }

        //обработчик закрытия формы
        private void OnFormClosing(object sender, System.ComponentModel.CancelEventArgs e) {

            SaveWindowProps(); //сохраняем размер и состояние окна

            if (!IsSaved) { //если настройки не сохранены выдаем предупреждение
                switch (System.Windows.Forms.MessageBox.Show(
                    "Настройки были изменены! Вы действительно хотите выйти?",
                    "Предупреждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                    )) {
                    case System.Windows.Forms.DialogResult.Yes:
                        //если все-таки нажали выход - закрываем без сохранений
                        e.Cancel = false;
                        break;
                    case System.Windows.Forms.DialogResult.No:
                        //отмена закрытия окна
                        e.Cancel = true;
                        break;
                }
            }
        }

        //показ окна о программе
        private void ShowAbout(object sender, RoutedEventArgs e) {
            new About().ShowDialog();
        }
        #endregion
    }
}
