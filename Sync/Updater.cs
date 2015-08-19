using System.Net; //библиотека для работы с сетью

namespace Sync {
    //статический класс
    public static class Updater {

        #region Properties
        //текущая версия программы
        public static string currentVersion = "1.1";
        //версия на сервере
        public static string newVersion { get; private set; }

        /// <summary>
        /// Проверка доступности обновлений на сервере
        /// </summary>
        public static bool UpdatesAvailable {
            get {
                //временно используем веб-клиент для получения строки из URL
                using (WebClient web = new WebClient()) {
                    //загружаем строку из документа
                    newVersion = web.DownloadString(
                    "http://manchenkoff.me/apps/gemino/gemino.ver"
                    );

                    //если текущая версия не совпадает с версией сервера - вернем true
                    return (currentVersion != newVersion) ? true : false;
                }
            }
        }

        /// <summary>
        /// Адрес для загрузки установщика
        /// </summary>
        public static string SetupURI {
            get {
                return "http://manchenkoff.me/apps/gemino/gemino.exe";
            }
        }
        #endregion

    }
}
