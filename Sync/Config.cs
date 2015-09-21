using System;
using System.IO; //для работы с файлами
using System.ComponentModel; //для работы привязки данных
using System.Collections.ObjectModel; //для привязки коллекций

namespace Sync {

    [Serializable]
    public class Config : INotifyPropertyChanged { //реализуем интерфейс для уведомлений об изменениях свойств

        #region Properties Event Logic
        [field:NonSerialized] //не сериализуем события при сохранении
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPopertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Properties
        private bool _autoload; //автозагрузка с ОС
        private bool _log; //писать лог-файлы
        private string _syncPath; //путь для синхронизаций по умолчанию
        private ObservableCollection<SyncObject> _folders; //коллекция папок для синхронизации

        /// <summary>
        /// Включена ли автозагрузка с Windows
        /// </summary>
        public bool Autoload {
            get {
                return _autoload;
            } set {
                if (_autoload != value) _autoload = value; OnPopertyChanged("Autoload");
            }
        }
        /// <summary>
        /// Записывать лог-файлы при изменениях
        /// </summary>
        public bool Log {
            get {
                return _log;
            } set {
                if (_log != value) _log = value; OnPopertyChanged("Log");
            }
        }
        /// <summary>
        /// Путь для всех синхронизаций по умолчанию
        /// </summary>
        public string SyncPath {
            get {
                return _syncPath;
            } set {
                if (_syncPath != value) _syncPath = value; OnPopertyChanged("SyncPath");
            }
        }
        /// <summary>
        /// Список всех синхронизируемых директорий
        /// </summary>
        public ObservableCollection<SyncObject> Folders {
            get {
                return _folders;
            } set {
                if (_folders != value) _folders = value; OnPopertyChanged("Folders");
            }
        }
        private string FilePath { get; set; } //путь файла настроек
        #endregion

        #region Constructors
        /// <summary>
        /// Создание экземпляра настроек с загрзукой данных из указанного пути
        /// </summary>
        /// <param name="filepath">Путь файла для чтения</param>
        public Config(string filepath) {
            FilePath = filepath; //кешируем путь
            Read(); //вызываем чтение содержимого файла
        }

        /// <summary>
        /// Создание нового экземпляра настроек с заданными параметрами
        /// </summary>
        /// <param name="autoload">Автозагрузка с Windows</param>
        /// <param name="log">Писать лог-файлы</param>
        /// <param name="syncpath">Путь для синхронизаций по умолчанию</param>
        public Config(bool autoload, bool log, string syncpath) {
            Autoload = autoload;
            Log = log;
            SyncPath = syncpath;
            Folders = new ObservableCollection<SyncObject>(); //инициализируем коллекцию директорий
        }
        #endregion

        #region Methods
        /// <summary>
        /// Записать изменения настроек в файл (путь по умолчанию)
        /// </summary>
        public void Write() {
            try {
                //генерируем путь - %User%/AppData/Roaming/Gemino/settings.ini
                string folder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Gemino"
                        );
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder); //если папки нет - создаем

                //Вызываем метод записи по пути
                WriteTo(Path.Combine(folder, "settings.ini"));

            } catch (Exception e) {
                //в случае исключения показываем диалоговое сообщение
                ShowError(e.Message);
            }
        }

        /// <summary>
        /// Метод записи экземпляра настроек в файл
        /// </summary>
        /// <param name="filepath">Путь для записи данных</param>
        public void WriteTo(string filepath) {
            try {
                //пишем массив байтов по пути filepath
                File.WriteAllBytes(
                    filepath,
                    Serializer.Serialize(this) //массив байтов, полученный из метода сериализации
                    );
            } catch (Exception e) {
                //в случае исключения выводим диалоговое сообщение
                ShowError(e.Message);
            }
        }

        /// <summary>
        /// Чтение файла настроек (путь по умолчанию)
        /// </summary>
        private void Read() {
            try {
                //создаем новый экземпляр с занесением десериализуемых данных
                Config ini = (Config)Serializer.Deserialize(
                    File.ReadAllBytes(FilePath)
                );

                //переназначаем значения на текущий экземпляр из ini
                Autoload = ini.Autoload; 
                Log = ini.Log;
                SyncPath = ini.SyncPath;
                Folders = ini.Folders;
            } catch (Exception e) {
                //в случае исключения выводим диалоговое сообщение
                ShowError(e.Message);
            }
        }

        /// <summary>
        /// Импорт настроек из указанного пути
        /// </summary>
        /// <param name="filepath">Путь файла настроек</param>
        public void LoadFrom(string filepath) {
            try {
                //читаем в новый экземпляр десериализованный файл
                Config ini = (Config)Serializer.Deserialize(
                    File.ReadAllBytes(filepath)
                );

                //переназначем все переменные на текущий объект
                Autoload = ini.Autoload;
                Log = ini.Log;
                SyncPath = ini.SyncPath;
                Folders = ini.Folders;
            } catch (Exception e) {
                //в случае исключения выводим диалоговое сообщение
                ShowError(e.Message);
            }
        }

        /// <summary>
        /// Перезагрузка файла настроек (при изменении содержимого)
        /// </summary>
        public void Reload() {
            Read(); //еще раз читаем настройки
        }

        /// <summary>
        /// Метод для показа ошибок при исключениях
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
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
