using System.Windows;
using Sync;

namespace Gemino.GUI {
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window {

        public About() {
            InitializeComponent();
            Init();
        }

        void Init() {
            appVersion.Content = string.Format("Версия {0}", Updater.currentVersion);
            appDescription.Text = Properties.Resources.Gemino_Updates;
        }
    }
}
