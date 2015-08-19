using System;
using System.Collections.Generic; //работа с коллекциями
using System.IO; //работа с файлами
using System.Threading; //работа с многопоточностью
using System.Threading.Tasks; //работа с задачами

namespace Sync {
    public class WatchDog {

        #region Properties
        private FileSystemWatcher Watcher; //наблюдатель за файловой системой
        private bool CanLogging = false; //состояние записи лог-файлов
        private List<Task> Tasks; //коллекция задач для каждого из WatchDog'ов
        private List<Thread> Threads; //коллекция потоков, в которых работают задачи

        /// <summary>
        /// Объект синхронизации, привязанный к текущему смотрителю
        /// </summary>
        public SyncObject Folder {
            get; private set;
        }

        /// <summary>
        /// Возвращает true если пользователь вручную остановил работу
        /// </summary>
        public bool UserStopped {
            get { return !Watcher.EnableRaisingEvents; }
        }

        /// <summary>
        /// Возвращает true если у текущего смотрителя есть активные задачи синхронизации
        /// </summary>
        /// <returns></returns>
        public bool HaveActiveTasks() {
            if (Tasks != null && Tasks.Count > 0) {
                return (Tasks.Find(x => x.Status == TaskStatus.Running) != null) ? true : false;
            } else return false;
        }
        #endregion

        #region Contructors
        /// <summary>
        /// Создание нового смотрителя по заданным параметрам
        /// </summary>
        /// <param name="syncObject">Объект синхронизации</param>
        /// <param name="canLog">Вести лог-файлы</param>
        public WatchDog(SyncObject syncObject, bool canLog) {
            Folder = syncObject; //назначем объект синхронизации
            Watcher = new FileSystemWatcher(Folder.Source.FullName); //запускаем внутреннего смотрителя
            Watcher.IncludeSubdirectories = true; //включая все подпапки
            //сообщать об изменении - имяФайла, последняяЗапись, имяПапки
            Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;

            //привязываем обработчики сигналов
            Watcher.Changed += Watcher_Changed;
            Watcher.Created += Watcher_Created;
            Watcher.Deleted += Watcher_Deleted;
            Watcher.Renamed += Watcher_Renamed;

            CanLogging = canLog; //назначаем состояние ведения лог-файлов
            
            //инициализируем коллекции задач и потоков
            Tasks = new List<Task>();
            Threads = new List<Thread>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Запуск наблюдения за изменениями
        /// </summary>
        public void Start() {
            //записываем в лог момент запуска смотрителя и имя папки
            Log(string.Format("'Наблюдатель' для '{0}' был запущен...", Folder.Name));
            //включаем наблюдение
            Watcher.EnableRaisingEvents = true;

            //запускаем задачу в отдельном потоке
            Tasks.Add(Task.Factory.StartNew(
                () => {
                    //добавляем поток задачи в список для управления
                    Threads.Add(Thread.CurrentThread);
                    //если папка "назначения" еще не существует
                    if (!Directory.Exists(Folder.Destination.ToString()))
                        //вызываем первую синхронизацию
                        SyncFolder(Folder.Source.ToString(), Folder.Destination.ToString());
                    //если существует, то проверяем изменения
                    else CheckSyncChanges(Folder);
                }
                ));
        }

        /// <summary>
        /// Остановка наблюдения за изменениями
        /// </summary>
        public void Stop() {
            //пишем в лог об остановке
            Log(string.Format("'Наблюдатель' для '{0}' был остановлен...", Folder.Name));
            //отключаем наблюдение
            Watcher.EnableRaisingEvents = false;
            //если задачи еще остались - вызываем массовую остановку потоков
            if (Tasks != null && Tasks.Count > 0) {
                Threads.ForEach(x => x.Abort());
            }
        }

        /// <summary>
        /// Запись сообщения в лог-файл
        /// </summary>
        /// <param name="message">Строка сообщения</param>
        private void Log(string message) {
            if (CanLogging) //если при старте разрешена запись
                Sync.Log.WriteString(message); //пишем строку в лог-файл
        }
        
        /// <summary>
        /// Проверка занятости файла
        /// </summary>
        /// <param name="filename">Путь к файлу</param>
        /// <returns></returns>
        private bool IsFileLocked(string filename) {
            try {
                //пытаемся открыть поток файла
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                    //если удалось - значит файл свободен
                    return false;
                }
            } catch (IOException) {
                //если не удалось, вернем false
                return true;
            }
        }
        #endregion

        #region Events
        //обработчик изменения файлов
        private void Watcher_Changed(object sender, FileSystemEventArgs e) {
            //пишем в лог что изменилось
            Log(string.Format("Объект '{0}' изменен", e.Name));

            //если это не папка
            if (!Directory.Exists(e.FullPath)) { 
                //создаем задачу в отдельном потоке
                Tasks.Add(Task.Factory.StartNew(
                    () => {
                        Threads.Add(Thread.CurrentThread); //записываем текущий поток в коллекцию
                        CreateObject(e.FullPath, Folder); //запускаем создание объекта
                    }
                    ));
            }
        }

        //обработчик переименования объектов
        private void Watcher_Renamed(object sender, RenamedEventArgs e) {
            //пишем что на что переименовалось
            Log(string.Format("Объект '{0}' был переименован в '{1}'", e.OldName, e.Name));

            //запускаем задачу в отдельном потоке
            Tasks.Add(Task.Factory.StartNew(
                () => {
                    Threads.Add(Thread.CurrentThread); //записываем текущий поток в коллекцию
                    RenameObject(e.OldFullPath, e.OldName, e.Name, Folder); //запускаем переименование
                }
                ));
        }

        //обработчик удаления объектов
        private void Watcher_Deleted(object sender, FileSystemEventArgs e) {
            //пишем что удалилось
            Log(string.Format("Объект '{0}' удален", e.Name));

            //запускаем задачу в отдельном потоке
            Tasks.Add(Task.Factory.StartNew(
                    () => {
                        Threads.Add(Thread.CurrentThread); //записываем текущий поток в коллекцию
                        DeleteObject(e.FullPath, Folder); //запускаем удаление
                    }
                    ));
        }

        //обработчик создания объектов
        private void Watcher_Created(object sender, FileSystemEventArgs e) {
            //пишем что создалось
            Log(string.Format("Объект '{0}' создан", e.Name));

            //запускаем задачу в отдельном потоке
            Tasks.Add(Task.Factory.StartNew(
                () => {
                    Threads.Add(Thread.CurrentThread); //записываем текущий поток в коллекцию

                    //если это папка
                    if (Directory.Exists(e.FullPath)) 
                        CreateFolder(e.FullPath, Folder); //запускаем создание папки
                    else //если нет
                        CreateObject(e.FullPath, Folder); //запускаем создание файлов
                }
                ));
        }
        #endregion

        #region SyncMethods
        /// <summary>
        /// Полная синхронизация директории
        /// </summary>
        /// <param name="from">Источник (откуда)</param>
        /// <param name="to">Назначение (куда)</param>
        void SyncFolder(string from, string to) {
            try {

                //получаем информацию о папке "источника"
                DirectoryInfo sourceDir = new DirectoryInfo(from);
                //получаем информацию о всех подпапках
                DirectoryInfo[] subDirs = sourceDir.GetDirectories();

                //если директория "назначение" еще не создана, то создаем
                if (!Directory.Exists(to)) Directory.CreateDirectory(to);

                //получаем список файлов в "источнике"
                FileInfo[] files = sourceDir.GetFiles();
                foreach (var file in files) {
                    //для всех циклом выполняем копирование в "назначение"
                    string temppath = Path.Combine(to, file.Name);
                    file.CopyTo(temppath, true);
                }

                //для каждой подпапки также выполняем полную синхронизацию
                foreach (var dir in subDirs) {
                    string temppath = Path.Combine(to, dir.Name);
                    SyncFolder(dir.FullName, temppath);
                }

            } catch (Exception e) {
                //если получаем исключение - выводим диалоговое сообщение
                Log(string.Format("Ошибка - {0}", e.Message));
                throw new Exception(e.Message);
            }
        }
        
        /// <summary>
        /// Метод переименования объектов
        /// </summary>
        /// <param name="fullpath">Полный путь к объекту</param>
        /// <param name="oldName">Старое имя</param>
        /// <param name="newName">Новое имя</param>
        /// <param name="folder">Объект синхронизации</param>
        void RenameObject(string fullpath, string oldName, string newName, SyncObject folder) {

            //получаем новый путь через замену части пути
            string sourcePath = fullpath.Replace(
                    folder.Source.ToString(), //путь источника
                    folder.Destination.ToString() //путь назначения
                    );

            //получаем новый путь с измененным именем
            string destPath = sourcePath.Replace(oldName, newName);

            try {
                if (Directory.Exists(sourcePath)) //если это папка
                    Directory.Move(sourcePath, destPath); //переименовываем в новое имя
                else File.Move(sourcePath, destPath); //если это файл, меняем его название
            } catch (Exception e) {
                //если получаем исключение - выводим ошибку
                Log(string.Format("Ошибка - {0}", e.Message));
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Метод удаления объекта
        /// </summary>
        /// <param name="fullpath">Полный путь объекта</param>
        /// <param name="folder">Объект синхронизации</param>
        void DeleteObject(string fullpath, SyncObject folder) {

            //получаем новый путь через замену части пути
            string sourcePath = fullpath.Replace(
                    folder.Source.ToString(), //путь источника
                    folder.Destination.ToString() //путь назначения
                    );

            try {
                if (Directory.Exists(sourcePath)) //если это папка
                    Directory.Delete(sourcePath, true); //удаляем вместе с содержимым
                else File.Delete(sourcePath); //иначе просто удаляем файл
            } catch (Exception e) {
                //если получаем исключение - выводим ошибку
                Log(string.Format("Ошибка - {0}", e.Message));
                throw new Exception(e.Message);
            }
        }

        void CreateObject(string fullpath, SyncObject folder) {

            //получаем новый путь через замену части пути
            string filename = fullpath.Replace(
                    folder.Source.ToString(), //путь источника
                    folder.Destination.ToString() //путь назначения
                    );

            try {
                //пока файл заблокирован системой или другим процессом
                while (IsFileLocked(fullpath))
                    Thread.Sleep(500); //ждем его освобождения

                //после чего выполняем копирование с заменой
                File.Copy(fullpath, filename, true);
            } catch (Exception e) {
                //если получаем исключение - выводим ошибку
                Log(string.Format("Ошибка - {0}", e.Message));
                throw new Exception(e.Message);
            }
        }

        void CreateFolder(string folderpath, SyncObject folder) {

            //получаем новый путь через замену части пути
            string foldername = folderpath.Replace(
                    folder.Source.ToString(), //путь источника
                    folder.Destination.ToString() //путь назначения
                    );

            //запускаем синхронизацию папки
            SyncFolder(folderpath, foldername);
        }

        /// <summary>
        /// Проверка изменений для синхронизации
        /// </summary>
        /// <param name="folder">Объект синхронизации</param>
        void CheckSyncChanges(SyncObject folder) {
            //получаем информацию об "источнике"
            DirectoryInfo sourceDir = new DirectoryInfo(folder.Source.ToString());
            //получаем информацию о "назначении"
            DirectoryInfo destDir = new DirectoryInfo(folder.Destination.ToString());

            //получаем списки файлов в обеих директориях
            List<FileInfo> sourceFiles = new List<FileInfo>(sourceDir.GetFiles());
            List<FileInfo> destFiles = new List<FileInfo>(destDir.GetFiles());

            //проверяем наличие всех файлов "источника" в "назначении"
            foreach (var file in sourceFiles) {
                if (destFiles.Contains(file)) continue; //если уже есть, пропускаем
                else CreateObject(file.FullName, folder); //иначе запускаем создание объекта
            }
            
            //получаем список подпапок "источника" и "назначения"
            List<DirectoryInfo> sourceSubDirs = new List<DirectoryInfo>(sourceDir.GetDirectories());
            List<DirectoryInfo> destSubDirs = new List<DirectoryInfo>(destDir.GetDirectories());

            //для каждой подпапки запускаем проверку
            foreach (var directory in sourceSubDirs) {
                if (destSubDirs.Contains(directory)) continue; //если уже есть, пропуск
                else CreateFolder(directory.FullName, folder); //иначе запускаем создание папки со всем содержимым
            }
        }
        #endregion
    }
}
