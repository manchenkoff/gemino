using System;
using System.IO; //работа с файлами
using System.Diagnostics; //работа с процессами
using System.Windows.Forms; //поддержка диалоговых сообщений
using System.Collections.Generic; //работа с коллекциями
using Sync; //библиотека классов

namespace Service {
    public class Tray {
        
        #region Properties
        private NotifyIcon _icon; //иконка в трее
        private ContextMenu _menu; //контекстное меню иконки
        private bool SettingsStarted = false; //запущены ли настройки

        private Config config; //файл конфигурации
        private List<WatchDog> Watchers; //коллекция 'наблюдателей'
        #endregion

        #region Constructors
        /// <summary>
        /// Базовый конструктор программы
        /// </summary>
        public Tray() {

            //создаем контекстное меню и присваиваем элементы с обработчиками
            _menu = new ContextMenu(new MenuItem[] {
                new MenuItem("Состояние - Ожидание"),
                new MenuItem("-"),
                new MenuItem("SYNC STATE", SyncProcesses), //остановка\запуск синхронизации
                new MenuItem("-"),
                new MenuItem("Проверить обновления...", CheckForUpdates),
                new MenuItem("Параметры", Settings),
                new MenuItem("Выход", Quit)
            });

            //делаем статус программы не для нажатий
            _menu.MenuItems[0].Enabled = false;

            //обработчик показа контекстного меню
            _menu.Popup += (s, e) => {
                //если есть 'наблюдатели'
                if (Watchers != null && Watchers.Count > 0) {
                    //выводим статус программы
                    _menu.MenuItems[0].Text = string.Format(
                        "Состояние - {0}",
                        //проверка активных задач
                        (Watchers.Find(x => x.HaveActiveTasks()) != null) ? "Синхронизация" : "Ожидание"
                        );

                    //включаем видимость пункта меню
                    _menu.MenuItems[2].Visible = _menu.MenuItems[3].Visible = true;
                    _menu.MenuItems[2].Text = "Остановить синхронизацию";
                } else if (Watchers != null && Watchers.TrueForAll(x => x.UserStopped)) {
                    //если же пользователь остановил синхронизацию, выводим запуск
                    _menu.MenuItems[2].Visible = _menu.MenuItems[3].Visible = true;
                    _menu.MenuItems[2].Text = "Запустить синхронизацию";
                }
            };

            //создаем иконку в трее
            _icon = new NotifyIcon {
                Icon = Properties.Resources.sync, //изображение
                Visible = true, //видимость
                Text = "Gemino - SmartSync", //заголовок
                ContextMenu = _menu //присваиваем меню
            };

            //открытие настроек по двойному щелчку
            _icon.DoubleClick += (s, e) => {
                Settings(s, e);
            };

            #region First Launch Program
            //действия при первом запуске программы
            if (Properties.Settings.Default.FirstLaunch) {
                //записываем в настройки что это был первый запуск
                Properties.Settings.Default.FirstLaunch = false;
                //записываем путь для последующего вызова настроек программы
                Properties.Settings.Default.ExePath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    "Gemino.exe"
                    );
                //сохраняем настройки
                Properties.Settings.Default.Save();
                //выводим сообщение
                ShowTip("Для изменения параметров кликните по иконке в трее");
            }
            #endregion
            Init(); //запускаем инициализацию программы
        }
        #endregion

        #region Methods
        /// <summary>
        /// Метод для показа всплывающих подсказок
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="icon">Тип иконки сообщения</param>
        public void ShowTip(string message, ToolTipIcon icon = ToolTipIcon.Info) {
            _icon.ShowBalloonTip(
                0, "Gemino - SmartSync",
                message,
                icon);
        }

        /// <summary>
        /// Метод автоматической проверки обновлений
        /// </summary>
        private void AutoUpdate() {
            try {

                //создаем таймер для проверки обновлений
                Timer interval = new Timer {
                    Interval = 21600000, //каждые 6 часов
                    Enabled = true //включаем его
                };
                //обработчик на проверку
                interval.Tick += (s, e) => {
                    //если есть обновления
                    if (Updater.UpdatesAvailable) {
                        //выводим сообщение
                        ShowTip("Доступна новая версия!");
                    }
                };
                //запускаем интервал
                interval.Start();
            } catch (Exception e) {
                //в случае ошибки записываем текст ошибки в лог
                Log(string.Format("Ошибка обновления - {0}", e.Message));
            }
        }

        /// <summary>
        /// Перезагрузка настроек
        /// </summary>
        public void Reload() {
            WatchersDispose(); //очитска 'наблюдателей'
            ShowTip("Настройки успешно обновлены"); //вывод сообщения
            Log("Обновление настроек"); //запись в лог-файл
            Init(); //повторная инициализация программы
        }

        /// <summary>
        /// Внутренний метод записи в лог-файл
        /// </summary>
        /// <param name="message">Текс сообщения</param>
        private void Log(string message) {
            if (config != null && config.Log) //если в настройках включена лог-запись
                Sync.Log.WriteString(message); //записываем строчку
        }

        /// <summary>
        /// Остановка\запуск 'наблюдателей'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncProcesses(object sender, EventArgs e) {
            //если 'наблюдателей' больше 0
            if (Watchers.Count > 0) {
                //выводим сообщение
                ShowTip("Ожидание остановки синхронизации...");
                //чистим коллекцию
                WatchersDispose();
                //пишем в лог
                Log("Пользователь остановил синхронизацию");
            } else {
                //если 'наблюдателей' нет
                //пишем в лог о запуске
                Log("Пользователь запустил синхронизацию");
                //повторно инициализируем приложение
                Init();
            }
        }

        //Обработчик проверки обновлений
        private void CheckForUpdates(object sender, EventArgs e) {
            //пишем в лог текущую версию
            Log(string.Format("Проверка обновлений...Текущая версия - v{0}", Updater.currentVersion));
            //проверяем доступность обновлений
            if (Updater.UpdatesAvailable) {
                //если есть обновления, выводим сообщение
                ShowTip("Доступна новая версия!");
                //и пишем в лог
                Log(string.Format("Доступна версия v{0}", Updater.newVersion));
            } else ShowTip("Обновлений нет"); //если нет - выводим сообщение

        }

        //обработчик настроек
        private void Settings(object sender, EventArgs e) {
            //если настройки уже запущены
            if (SettingsStarted) {
                return; //возврат из функции
            } else {
                try {
                    //пишем в лог о файле запуска
                    Log(string.Format("Запуск параметров ({0})...", Properties.Settings.Default.ExePath));
                    //создаем процесс для запуска по пути
                    Process settings = new Process {
                        StartInfo = new ProcessStartInfo(
                            Properties.Settings.Default.ExePath
                        )
                    };

                    //запускаем процесс
                    settings.Start();
                    //записываем запуск настроек
                    SettingsStarted = true;
                    //дожидаемся выхода
                    settings.WaitForExit();
                    //если настройки закрыты и код выхода равен 1
                    if (settings.HasExited && settings.ExitCode == 1)
                        Reload(); //перезагружаем настройки
                    else if (settings.HasExited && settings.ExitCode == 2) //если код выхода 2
                        Quit(sender, e); //значит закрываем приложение для обновления
                    //меняем переменную открытия настроек
                    SettingsStarted = false;
                } catch (Exception ex) {
                    //если получили исключение, пишем в лог ошибку
                    Log(string.Format("Ошибка - {0}", ex.Message));
                    //и выводим сообщение
                    ShowTip(ex.Message, ToolTipIcon.Error);
                }
            }
        }

        //обработчик выхода из программы
        private void Quit(object sender, EventArgs e) {
            Log("Завершение программы ... Остановка 'наблюдателей' ...");
            _icon.Dispose(); //очищаем ресурсы иконки в трее
            WatchersDispose(); //очищаем ресурсы 'наблюдателей'
            Log("### Приложение завершено, все ресурсы успешно очищены! ###");
            //закрываем приложение
            Application.Exit();
        }

        /// <summary>
        /// Инициализация приложения
        /// </summary>
        private void Init() {
            try {
                //пишем в лог о старте программы
                Log("### Загрузка приложения ###");
                //получаем путь к файлу настроек
                string path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Gemino",
                        "settings.ini"
                        );
                //если файла нет - создаем базовый
                if (!File.Exists(path)) ConfigInit();
                //загружаем объект настроек
                config = new Config(path);
                //проверяем автозагрузку приложения
                AutoloadCheck();
                //инициализируем коллекцию 'наблюдателей'
                Watchers = new List<WatchDog>();
                //циклом для всех объектов синхронизации назначаем 'наблюдателя'
                foreach (var syncFolder in config.Folders) {
                    WatchDog watcher = new WatchDog(syncFolder, config.Log);
                    Watchers.Add(watcher); //добавляем в общую коллекцию
                    //запускаем наблюдение
                    watcher.Start();
                }
                //записываем в лог количество запущенных 'наблюдателей'
                if (Watchers.Count > 0) Log(string.Format("Запущено {0} 'наблюдателей'", Watchers.Count));
                //проверяем обновления
                AutoUpdate();
                
            } catch (Exception e) {
                //если получили исключение - пишем в лог-файл
                Log(string.Format("Ошибка - {0}", e.Message));
                //и выводим сообщение
                ShowTip(e.Message, ToolTipIcon.Error);
            }
        }

        /// <summary>
        /// Инициализация базового файла настроек
        /// </summary>
        /// <param name="path">Путь для сохранения</param>
        private void ConfigInit() {
            //создаем объект настроек
            config = new Config(true, false, Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), //путь для сохранения по умолчанию
                        "Gemino")
                        );
            //записывем в место по умолчанию
            config.Write();
        }

        /// <summary>
        /// Проверка автозагрузки программы
        /// </summary>
        private void AutoloadCheck() {
            //получаем ключ в реестре
            Microsoft.Win32.RegistryKey autorunKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true
                );
            //если в настройках включена автозагрузка
            if (config.Autoload) {
                if (autorunKey.GetValue("Gemino") == null)
                    //добавляем ключ в реестр
                    autorunKey.SetValue("Gemino", Application.ExecutablePath.ToString());
            } else { //иначе
                if (autorunKey.GetValue("Gemino") != null)
                    //удаляем ключ в реестре
                    autorunKey.DeleteValue("Gemino", false);
            }
        }

        /// <summary>
        /// Очистка ресурсов 'наблюдателей'
        /// </summary>
        private void WatchersDispose() {
            //остановка всех объектов и чистка коллекции
            if (Watchers != null && Watchers.Count > 0) Watchers.ForEach(x => x.Stop()); Watchers.Clear();
        }
    }
    #endregion
}
