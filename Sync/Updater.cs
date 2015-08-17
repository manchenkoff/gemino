using System.Net;

namespace Sync {
    public static class Updater {

        #region Properties
        public static string currentVersion = "1.0";
        public static string newVersion { get; private set; }

        public static bool UpdatesAvailable {
            get {
                using (WebClient web = new WebClient()) {

                    newVersion = web.DownloadString(
                    "http://manchenkoff.me/apps/gemino/gemino.ver"
                    );

                    return (currentVersion != newVersion) ? true : false;
                }
            }
        }

        public static string SetupURI {
            get {
                return "http://manchenkoff.me/apps/gemino/gemino.exe";
            }
        }
        #endregion

    }
}
