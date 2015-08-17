using System;
using System.IO;
using System.ComponentModel;

namespace Sync {

    [Serializable]
    public class SyncObject : INotifyPropertyChanged {

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPopertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties
        private string _name;
        private DirectoryInfo _source, _destination;

        public string Name {
            get { return _name; }
            set {
                if (_name != value) {
                    _name = value;
                    OnPopertyChanged("Name");
                }
            }
        }
        public DirectoryInfo Source {
            get { return _source; }
            set {
                if (_source != value) {
                    _source = value;
                    OnPopertyChanged("Source");
                }
            }
        }
        public DirectoryInfo Destination {
            get { return _destination; }
            set {
                if (_destination != value) {
                    _destination = value;
                    OnPopertyChanged("Destination");
                }
            }
        }
        #endregion

        #region Constructors
        public SyncObject(string name, string sourceFolder, string destinationFolder) {
            try {
                Name = name;

                Source = new DirectoryInfo(sourceFolder);
                if (!Source.Exists) System.Windows.Forms.MessageBox.Show(
                    string.Format("Директория {0} не найдена!", 
                    Source.FullName)
                    );

                Destination = new DirectoryInfo(Path.Combine(destinationFolder, name));

            } catch (Exception e) {
                System.Windows.Forms.MessageBox.Show(
                    e.Message, 
                    "Gemino SmartSync - Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error
                    );
            }
        }
        #endregion
    }
}