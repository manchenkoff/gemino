using System;
using System.Windows.Forms;

namespace Service {

    #region Main Void
    static class App {
        [STAThread]
        static void Main() {

            Tray icon = new Tray();
            Application.Run();

        }
    }
    #endregion
}
