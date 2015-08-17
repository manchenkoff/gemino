using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sync {
    public class WatchDog {

        #region Properties
        private FileSystemWatcher Watcher;
        private bool CanLogging = false;
        private List<Task> Tasks;
        private List<Thread> Threads;

        public SyncObject Folder {
            get; private set;
        }
        public bool UserStopped {
            get { return !Watcher.EnableRaisingEvents; }
        }

        public bool HaveActiveTasks() {
            if (Tasks != null && Tasks.Count > 0) {
                return (Tasks.Find(x => x.Status == TaskStatus.Running) != null) ? true : false;
            } else return false;
        }
        #endregion

        #region Contructors
        public WatchDog(SyncObject syncObject, bool canLog) {
            Folder = syncObject;
            Watcher = new FileSystemWatcher(Folder.Source.FullName);
            Watcher.IncludeSubdirectories = true;
            Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;

            Watcher.Changed += Watcher_Changed;
            Watcher.Created += Watcher_Created;
            Watcher.Deleted += Watcher_Deleted;
            Watcher.Renamed += Watcher_Renamed;

            CanLogging = canLog;
            
            Tasks = new List<Task>();
            Threads = new List<Thread>();
        }
        #endregion

        #region Methods
        public void Start() {
            Log(string.Format("'Наблюдатель' для '{0}' был запущен...", Folder.Name));
            Watcher.EnableRaisingEvents = true;

            Tasks.Add(Task.Factory.StartNew(
                () => {
                    Threads.Add(Thread.CurrentThread);
                    if (!Directory.Exists(Folder.Destination.ToString()))
                        SyncFolder(Folder.Source.ToString(), Folder.Destination.ToString());
                    else CheckSyncChanges(Folder);
                }
                ));
        }

        public void Stop() {
            Log(string.Format("'Наблюдатель' для '{0}' был остановлен...", Folder.Name));
            Watcher.EnableRaisingEvents = false;
            if (Tasks != null && Tasks.Count > 0) {
                Threads.ForEach(x => x.Abort());
            }
        }

        private void Log(string message) {
            if (CanLogging)
                Sync.Log.WriteString(message);
        }
        
        private bool IsFileLocked(string filename) {
            try {
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                    return false;
                }
            } catch (IOException) {
                return true;
            }
        }
        #endregion

        #region Events
        private void Watcher_Changed(object sender, FileSystemEventArgs e) {
            Log(string.Format("Объект '{0}' изменен", e.Name));

            if (!Directory.Exists(e.FullPath)) {
                Tasks.Add(Task.Factory.StartNew(
                    () => {
                        Threads.Add(Thread.CurrentThread);
                        CreateObject(e.FullPath, Folder);
                    }
                    ));
            }
        }
        private void Watcher_Renamed(object sender, RenamedEventArgs e) {
            Log(string.Format("Объект '{0}' был переименован в '{1}'", e.OldName, e.Name));

            Tasks.Add(Task.Factory.StartNew(
                () => {
                    Threads.Add(Thread.CurrentThread);
                    RenameObject(e.OldFullPath, e.OldName, e.Name, Folder);
                }
                ));
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e) {
            Log(string.Format("Объект '{0}' удален", e.Name));

            Tasks.Add(Task.Factory.StartNew(
                    () => {
                        Threads.Add(Thread.CurrentThread);
                        DeleteObject(e.FullPath, Folder);
                    }
                    ));
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e) {
            Log(string.Format("Объект '{0}' создан", e.Name));

            Tasks.Add(Task.Factory.StartNew(
                () => {
                    Threads.Add(Thread.CurrentThread);
                    if (Directory.Exists(e.FullPath))
                        CreateFolder(e.FullPath, Folder);
                    else
                        CreateObject(e.FullPath, Folder);
                }
                ));
        }
        #endregion

        #region SyncMethods
        void SyncFolder(string from, string to) {
            try {

                DirectoryInfo sourceDir = new DirectoryInfo(from);
                DirectoryInfo[] subDirs = sourceDir.GetDirectories();

                if (!Directory.Exists(to)) Directory.CreateDirectory(to);

                FileInfo[] files = sourceDir.GetFiles();
                foreach (var file in files) {
                    string temppath = Path.Combine(to, file.Name);
                    file.CopyTo(temppath, true);
                }

                foreach (var dir in subDirs) {
                    string temppath = Path.Combine(to, dir.Name);
                    SyncFolder(dir.FullName, temppath);
                }

            } catch (Exception e) {
                throw new Exception(e.Message);
            }
        }
        
        void RenameObject(string fullapth, string oldName, string newName, SyncObject folder) {

            string sourcePath = fullapth.Replace(
                    folder.Source.ToString(),
                    folder.Destination.ToString()
                    );

            string destPath = sourcePath.Replace(oldName, newName);

            try {
                if (Directory.Exists(sourcePath))
                    Directory.Move(sourcePath, destPath);
                else File.Move(sourcePath, destPath);
            } catch (Exception e) {
                throw new Exception(e.Message);
            }
        }

        void DeleteObject(string fullpath, SyncObject folder) {

            string sourcePath = fullpath.Replace(
                    folder.Source.ToString(),
                    folder.Destination.ToString()
                    );

            try {
                if (Directory.Exists(sourcePath))
                    Directory.Delete(sourcePath, true);
                else File.Delete(sourcePath);
            } catch (Exception e) {
                throw new Exception(e.Message);
            }
        }

        void CreateObject(string fullpath, SyncObject folder) {

            string filename = fullpath.Replace(
                    folder.Source.ToString(),
                    folder.Destination.ToString()
                    );

            try {
                while (IsFileLocked(fullpath))
                    Thread.Sleep(500);

                File.Copy(fullpath, filename, true);
            } catch (Exception e) {
                throw new Exception(e.Message);
            }
        }

        void CreateFolder(string folderpath, SyncObject folder) {

            string foldername = folderpath.Replace(
                    folder.Source.ToString(),
                    folder.Destination.ToString()
                    );

            SyncFolder(folderpath, foldername);
        }

        void CheckSyncChanges(SyncObject folder) {
            DirectoryInfo sourceDir = new DirectoryInfo(folder.Source.ToString());
            DirectoryInfo destDir = new DirectoryInfo(folder.Destination.ToString());

            List<FileInfo> sourceFiles = new List<FileInfo>(sourceDir.GetFiles());
            List<FileInfo> destFiles = new List<FileInfo>(destDir.GetFiles());

            foreach (var file in sourceFiles) {
                if (destFiles.Contains(file)) continue;
                else CreateObject(file.FullName, folder);
            }
            
            List<DirectoryInfo> sourceSubDirs = new List<DirectoryInfo>(sourceDir.GetDirectories());
            List<DirectoryInfo> destSubDirs = new List<DirectoryInfo>(destDir.GetDirectories());
            foreach (var directory in sourceSubDirs) {
                if (destSubDirs.Contains(directory)) continue;
                else CreateFolder(directory.FullName, folder);
            }
        }
        #endregion
    }
}
