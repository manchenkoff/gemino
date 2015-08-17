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
        private SyncObject syncObject;
        private Config config;
        private FolderBrowserDialog folderBrowse;
        #endregion

        #region Constructors
        public FolderEditDialog(Config configuration) {
            InitializeComponent();
            config = configuration;
            DestinationPathTextbox.Text = config.SyncPath;
        }

        public FolderEditDialog(SyncObject sync) {
            InitializeComponent();
            SourcePathTextbox.Text = sync.Source.FullName;
            DestinationPathTextbox.Text = sync.Destination.FullName;
            NameTextbox.Text = sync.Name;
            syncObject = sync;
        }
        #endregion

        #region Methods
        private void BrowseSource(object sender, RoutedEventArgs e) {
            folderBrowse = new FolderBrowserDialog();
            folderBrowse.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowse.Description = "Выберите директорию для синхронизации...";
            
            if (folderBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                SourcePathTextbox.Text = folderBrowse.SelectedPath;
                NameTextbox.Text = folderBrowse.SelectedPath.Substring(
                    folderBrowse.SelectedPath.LastIndexOf('\\') + 1
                    );
            }
        }

        private void BrowseDestination(object sender, RoutedEventArgs e) {

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
                DestinationPathTextbox.Text = folderBrowse.SelectedPath;
            }
        }

        private void Apply(object sender, RoutedEventArgs e) {
            if (syncObject != null) {
                syncObject.Name = NameTextbox.Text;
                syncObject.Source = new System.IO.DirectoryInfo(SourcePathTextbox.Text);
                syncObject.Destination = new System.IO.DirectoryInfo(
                    System.IO.Path.Combine(
                        DestinationPathTextbox.Text,
                        NameTextbox.Text
                        ));
            } else {
                syncObject = new SyncObject(
                    NameTextbox.Text, SourcePathTextbox.Text, DestinationPathTextbox.Text
                    );
                config.Folders.Add(syncObject);
            }
            DialogResult = true;
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e) {
            Close();
        }
        #endregion
    }
}
