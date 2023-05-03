using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SyncDB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string strArgs = "";
            if (e.Args.Length == 1) strArgs = e.Args[0];
            MainWindow mainWindow = new MainWindow(strArgs);
            mainWindow.Show();
        }
    }
}
