using System;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Sync {

    [Serializable]
    public class Config : INotifyPropertyChanged {

        #region Properties Event Logic
        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPopertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Properties
        private bool _autoload;
        private bool _log;
        private string _syncPath;
        private ObservableCollection<SyncObject> _folders;

        public bool Autoload {
            get {
                return _autoload;
            } set {
                if (_autoload != value) _autoload = value; OnPopertyChanged("Autoload");
            }
        }
        public bool Log {
            get {
                return _log;
            } set {
                if (_log != value) _log = value; OnPopertyChanged("Log");
            }
        }
        public string SyncPath {
            get {
                return _syncPath;
            } set {
                if (_syncPath != value) _syncPath = value; OnPopertyChanged("SyncPath");
            }
        }
        public ObservableCollection<SyncObject> Folders {
            get {
                return _folders;
            } set {
                if (_folders != value) _folders = value; OnPopertyChanged("Folders");
            }
        }
        private string FilePath { get; set; }
        #endregion

        #region Constructors
        public Config(string filepath) {
            FilePath = filepath;
            Read();
        }

        public Config(bool autoload, bool log, string syncpath) {
            Autoload = autoload;
            Log = log;
            SyncPath = syncpath;
            Folders = new ObservableCollection<SyncObject>();
        }
        #endregion

        #region Methods
        public void Write() {
            try {
                string folder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Gemino"
                        );
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                WriteTo(Path.Combine(folder, "settings.ini"));

            } catch (Exception e) {
                ShowError(e.Message);
            }
        }

        public void WriteTo(string filepath) {
            try {
                File.WriteAllBytes(
                    filepath,
                    Serializer.Serialize(this)
                    );
            } catch (Exception e) {
                ShowError(e.Message);
            }
        }

        private void Read() {
            try {
                Config ini = (Config)Serializer.Deserialize(
                    File.ReadAllBytes(FilePath)
                );

                Autoload = ini.Autoload;
                Log = ini.Log;
                SyncPath = ini.SyncPath;
                Folders = ini.Folders;
            } catch (Exception e) {
                ShowError(e.Message);
            }
        }

        public void LoadFrom(string filepath) {
            try {
                Config ini = (Config)Serializer.Deserialize(
                    File.ReadAllBytes(filepath)
                );

                Autoload = ini.Autoload;
                Log = ini.Log;
                SyncPath = ini.SyncPath;
                Folders = ini.Folders;
            } catch (Exception e) {
                ShowError(e.Message);
            }
        }

        public void Reload() {
            Read();
        }

        private void ShowError(string message) {
            System.Windows.Forms.MessageBox.Show(
                    message,
                    "Gemino SmartSync - Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                    );
        }
        #endregion

    }
}
