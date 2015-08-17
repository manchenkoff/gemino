using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using Sync;

namespace Service {
    public class Tray {
        
        #region Properties
        private NotifyIcon _icon;
        private ContextMenu _menu;

        private Config config;
        private List<WatchDog> Watchers;
        #endregion

        #region Constructors
        public Tray() {

            _menu = new ContextMenu(new MenuItem[] {
                new MenuItem("Состояние - Ожидание"),
                new MenuItem("-"),
                new MenuItem("SYNC STATE", SyncProcesses),
                new MenuItem("-"),
                new MenuItem("Проверить обновления...", CheckForUpdates),
                new MenuItem("Параметры", Settings),
                new MenuItem("Выход", Quit)
            });

            _menu.Popup += (s, e) => {
                if (Watchers != null && Watchers.Count > 0) {
                    _menu.MenuItems[0].Text = string.Format(
                        "Состояние - {0}",
                        (Watchers.Find(x => x.HaveActiveTasks()) != null) ? "Синхронизация" : "Ожидание"
                        );

                    _menu.MenuItems[2].Visible = _menu.MenuItems[3].Visible = true;
                    _menu.MenuItems[2].Text = "Остановить синхронизацию";
                } else if (Watchers != null && Watchers.TrueForAll(x => x.UserStopped)) {
                    _menu.MenuItems[2].Visible = _menu.MenuItems[3].Visible = true;
                    _menu.MenuItems[2].Text = "Запустить синхронизацию";
                }
            };

            _icon = new NotifyIcon {
                Icon = Properties.Resources.sync,
                Visible = true,
                Text = "Gemino - SmartSync",
                ContextMenu = _menu
            };

            #region First Launch Program
            if (Properties.Settings.Default.FirstLaunch) {
                Properties.Settings.Default.FirstLaunch = false;
                Properties.Settings.Default.Save();
                ShowTip("Для изменения параметров кликните по иконке в трее");
            }
            #endregion
            Init();
        }
        #endregion

        #region Methods
        public void ShowTip(string message, ToolTipIcon icon = ToolTipIcon.Info) {
            _icon.ShowBalloonTip(
                0, "Gemino - SmartSync",
                message,
                icon);
        }

        public void Reload() {
            WatchersDispose();
            ShowTip("Настройки успешно обновлены");
            Log("Обновление настроек");
            Init();
        }

        private void Log(string message) {
            if (config != null && config.Log)
                Sync.Log.WriteString(message);
        }

        private void SyncProcesses(object sender, EventArgs e) {
            if (Watchers.Count > 0) {
                ShowTip("Ожидание остановки синхронизации...");
                WatchersDispose();
                Log("Пользователь остановил синхронизацию");
            } else {
                Log("Пользователь запустил синхронизацию");
                Init();
            }
        }

        private void CheckForUpdates(object sender, EventArgs e) {

            Log(string.Format("Проверка обновлений...Текущая версия - v{0}", Updater.currentVersion));
            if (Updater.UpdatesAvailable) {
                ShowTip("Доступна новая версия!");
                Log(string.Format("Доступна версия v{0}", Updater.newVersion));
            } else ShowTip("Обновлений нет");

        }

        private void Settings(object sender, EventArgs e) {
            try {
                Log("Запуск параметров...");
                Process settings = new Process {
                    StartInfo = new ProcessStartInfo(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "Gemino.exe"
                        ))
                };

                settings.Start();
                settings.WaitForExit();
                if (settings.HasExited && settings.ExitCode == 1)
                    Reload();
                else if (settings.HasExited && settings.ExitCode == 2)
                    Quit(sender, e);

            } catch (Exception ex) {
                ShowTip(ex.Message, ToolTipIcon.Error);
            }
        }

        private void Quit(object sender, EventArgs e) {
            Log("Завершение программы ... Остановка 'наблюдателей' ...");
            _icon.Dispose();
            WatchersDispose();
            Log("### Приложение завершено, все ресурсы успешно очищены! ###");
            Application.Exit();
        }

        private void Init() {
            try {

                Log("### Загрузка приложения ###");

                string path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Gemino",
                        "settings.ini"
                        );

                if (!File.Exists(path)) ConfigInit(path);

                config = new Config(path);
                AutoloadCheck();

                Watchers = new List<WatchDog>();

                foreach (var syncFolder in config.Folders) {
                    WatchDog watcher = new WatchDog(syncFolder, config.Log);
                    Watchers.Add(watcher);

                    watcher.Start();
                }

                if (Watchers.Count > 0) Log(string.Format("Запущено {0} 'наблюдателей'", Watchers.Count));
                
            } catch (Exception e) {
                ShowTip(e.Message, ToolTipIcon.Error);
            }
        }

        private void ConfigInit(string path) {

            config = new Config(true, false, Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Gemino")
                        );

            config.Write();
        }

        private void AutoloadCheck() {

            Microsoft.Win32.RegistryKey autorunKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true
                );

            if (config.Autoload) {
                if (autorunKey.GetValue("Gemino") == null)
                    autorunKey.SetValue("Gemino", Application.ExecutablePath.ToString());
            } else {
                if (autorunKey.GetValue("Gemino") != null)
                    autorunKey.DeleteValue("Gemino", false);
            }
        }

        private void WatchersDispose() {
            if (Watchers != null && Watchers.Count > 0) Watchers.ForEach(x => x.Stop()); Watchers.Clear();
        }
    }
    #endregion
}
