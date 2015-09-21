using System;
using System.IO; //для работы с файлами
using System.Linq; //для сортировки списков

namespace Sync {
    public static class Log {

        #region Properties
        /// <summary>
        /// Путь для хранения лог-файлов
        /// </summary>
        static string FolderPath { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Метод записи строки в лог-файл
        /// </summary>
        /// <param name="message">Строка сообщения для записи</param>
        public static void WriteString(string message) {
            
            //Проверяем существование папки для лог-файлов
            CheckFolderExists();

            //Генерируем название файла в зависимости от текущей даты
            string filename = Path.Combine(
                FolderPath, //основной путь
                string.Format(
                    "{0}.{1}.{2}.log", //формат - ДД.ММ.ГГГГ
                    DateTime.Now.Day, 
                    DateTime.Now.Month, 
                    DateTime.Now.Year)
                );

            //пытаемся добавить строку в файл
            try {
                if (!File.Exists(filename)) File.Create(filename).Close(); //если файла нет - создаем и закрываем

                //добавляем строку в конец файла
                File.AppendAllText(filename, string.Format(
                    "{0} - {1}\r\n", DateTime.Now, message //формат = ДД.ММ.ГГГГ ЧЧ:ММ:СС - сообщение
                    ));
            } catch (Exception e) { //если получаем исключение
                //выводим его в диалоговом сообщении
                System.Windows.Forms.MessageBox.Show(
                    e.Message,
                    "Gemino SmartSync - Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                    );
            }
        }

        /// <summary>
        /// Проверка существования папки для хранения лог-файлов
        /// </summary>
        private static void CheckFolderExists() {

            //Генерируем путь для лог-файлов и кешируем в перменную FolderPath
            FolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), //%user%/AppData/Roaming
                "Gemino/Logs" //Папка лог-файлов
                );

            //если папка еще не создана
            if (!Directory.Exists(FolderPath)) {
                //создаем по кешированному пути
                Directory.CreateDirectory(FolderPath);
            } else { //если создана
                //получаем список файлов
                var files = new DirectoryInfo(FolderPath).GetFiles();
                //сортируем по дате создания и пропускаем первые 3 файла
                foreach (var file in files.OrderByDescending(x => x.CreationTime).Skip(3))
                    //4-й файл удаляется как самый старый
                    file.Delete();
            }
        }
        #endregion

    }
}
