using System;
using System.IO; //работа с файлами
using System.ComponentModel; //работа привязки данных

namespace Sync {

    [Serializable]
    public class SyncObject : INotifyPropertyChanged {

        [field: NonSerialized] //не сериализуем событие при сохранении
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPopertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties
        private string _name;
        private DirectoryInfo _source, _destination;

        /// <summary>
        /// Название папки синхронизации
        /// </summary>
        public string Name {
            get { return _name; }
            set {
                if (_name != value) {
                    _name = value;
                    OnPopertyChanged("Name");
                }
            }
        }
        /// <summary>
        /// Исходный путь синхронизации
        /// </summary>
        public DirectoryInfo Source {
            get { return _source; }
            set {
                if (_source != value) {
                    _source = value;
                    OnPopertyChanged("Source");
                }
            }
        }
        /// <summary>
        /// Путь назначения для копирования
        /// </summary>
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
        /// <summary>
        /// Создание нового экземпляра синхронизиуеромого объекта с заданными параметрами
        /// </summary>
        /// <param name="name">Название папки</param>
        /// <param name="sourceFolder">Источник (Откуда копировать)</param>
        /// <param name="destinationFolder">Назначение (Куда копировать)</param>
        public SyncObject(string name, string sourceFolder, string destinationFolder) {
            try {
                Name = name; //назначаем имя

                //получаем информацию о папке "источника"
                Source = new DirectoryInfo(sourceFolder);
                if (!Source.Exists) System.Windows.Forms.MessageBox.Show(
                    //выводим ошибку если папки нет
                    string.Format("Директория {0} не найдена!", 
                    Source.FullName)
                    );

                //получаем информацию о папки "назначения"
                Destination = new DirectoryInfo(Path.Combine(destinationFolder, name));

            } catch (Exception e) {
                //в случае исключения выводим диалоговое сообщение
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