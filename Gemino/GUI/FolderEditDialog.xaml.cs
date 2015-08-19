using Sync;
using System;
using System.Windows;
using System.Windows.Forms;

namespace Gemino.GUI {
    /// <summary>
    /// Interaction logic for FolderEditDialog.xaml
    /// </summary>
    public partial class FolderEditDialog : Window {

        #region Properties
        private SyncObject syncObject; //объект синхронизации
        private Config config; //настройки программы
        private FolderBrowserDialog folderBrowse; //диалог выбора папки
        #endregion

        #region Constructors
        /// <summary>
        /// Базовый конструктор диалога
        /// </summary>
        /// <param name="configuration">Объект настроек программы</param>
        public FolderEditDialog(Config configuration) {
            InitializeComponent(); //загружаем вид
            config = configuration; //назначем настройки
            DestinationPathTextbox.Text = config.SyncPath; //заполняем путь по умолчанию
        }

        /// <summary>
        /// Конструктор для редактирования существующего объекта синхронизации
        /// </summary>
        /// <param name="sync">Объект синхронизации</param>
        public FolderEditDialog(SyncObject sync) {
            InitializeComponent(); //загружаем вид
            SourcePathTextbox.Text = sync.Source.FullName; //заполняем источник
            DestinationPathTextbox.Text = sync.Destination.FullName; //заполняем назначение 
            NameTextbox.Text = sync.Name; //заполняем имя
            syncObject = sync; //назначаем объект синхронизации для сохранения изменений
        }
        #endregion

        #region Methods
        //выбор источника
        private void BrowseSource(object sender, RoutedEventArgs e) {
            folderBrowse = new FolderBrowserDialog();
            folderBrowse.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowse.Description = "Выберите директорию для синхронизации...";
            
            if (folderBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                SourcePathTextbox.Text = folderBrowse.SelectedPath; //выбранный путь
                //вытаскиваем имя последней папки
                NameTextbox.Text = folderBrowse.SelectedPath.Substring(
                    folderBrowse.SelectedPath.LastIndexOf('\\') + 1
                    );
            }
        }

        //выбор назначения
        private void BrowseDestination(object sender, RoutedEventArgs e) {
            //если путь уже назначен - выводим предупреждение
            if (DestinationPathTextbox.Text != string.Empty && DestinationPathTextbox.Text != config.SyncPath) {
                System.Windows.Forms.MessageBox.Show(
                    "Если Вы измените 'Назначение' созданная ранне папка удалена не будет",
                    "Gemino SmartSync - Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                    );
            }

            folderBrowse = new FolderBrowserDialog();
            folderBrowse.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowse.Description = "Выберите путь для сохранения копии...";

            if (folderBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                DestinationPathTextbox.Text = folderBrowse.SelectedPath; //выбранный путь
            }
        }

        //кнопка сохранения изменений
        private void Apply(object sender, RoutedEventArgs e) {
            if (syncObject != null) { //если объект синхронизации был передан
                //назначаем ему все параметры
                syncObject.Name = NameTextbox.Text;
                syncObject.Source = new System.IO.DirectoryInfo(SourcePathTextbox.Text);
                syncObject.Destination = new System.IO.DirectoryInfo(
                    System.IO.Path.Combine(
                        DestinationPathTextbox.Text,
                        NameTextbox.Text
                        ));
            } else {
                //иначе создаем новый объект
                syncObject = new SyncObject(
                    NameTextbox.Text, SourcePathTextbox.Text, DestinationPathTextbox.Text
                    );
                //и добавляем в общую коллекцию настроек
                config.Folders.Add(syncObject);
            }
            //возвращаем результат диалога
            DialogResult = true;
            //закрываем форму
            Close();
        }

        //кнопка отмены
        private void Cancel(object sender, RoutedEventArgs e) {
            Close(); //закрываем форму без сохранений
        }
        #endregion
    }
}
