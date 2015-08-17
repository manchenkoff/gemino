using System;
using System.IO;
using System.Linq;

namespace Sync {
    public static class Log {

        #region Properties
        static string FolderPath { get; set; }
        #endregion

        #region Methods
        public static void WriteString(string message) {

            CheckFolderExists();

            string filename = Path.Combine(
                FolderPath,
                string.Format(
                    "{0}.{1}.{2}.log", 
                    DateTime.Now.Day, 
                    DateTime.Now.Month, 
                    DateTime.Now.Year)
                );
            try {
                if (!File.Exists(filename)) File.Create(filename).Close();

                File.AppendAllText(filename, string.Format(
                    "{0} - {1}\r\n", DateTime.Now, message
                    ));
            } catch (Exception e) {
                System.Windows.Forms.MessageBox.Show(
                    e.Message,
                    "Gemino SmartSync - Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                    );
            }
        }

        private static void CheckFolderExists() {

            FolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Gemino/Logs"
                );

            if (!Directory.Exists(FolderPath)) {
                Directory.CreateDirectory(FolderPath);
            } else {
                var files = new DirectoryInfo(FolderPath).GetFiles();

                foreach (var file in files.OrderByDescending(x => x.CreationTime).Skip(3))
                    file.Delete();
            }
        }
        #endregion

    }
}
