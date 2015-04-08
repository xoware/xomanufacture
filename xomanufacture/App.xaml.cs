using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace xomanufacture
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainWindow AppWindow;
        private AController context;

        public App()
        {
            AppWindow = new MainWindow();
            context = new AController(this, AppWindow);
            AppWindow.DataContext = context;
            AppWindow.Show();
        }
    }
}
