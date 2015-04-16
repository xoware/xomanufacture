using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace xomanufacture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StartHandlerChain = new List<StartHandlerType>();
            ContentRendered += (o, e) => ContentRenderedHandler(o, e);
        }

        private List<StartHandlerType> StartHandlerChain;
        private void ContentRenderedHandler(Object o, EventArgs e)
        {
            foreach (StartHandlerType element in StartHandlerChain)
            {
                element(o, e);
            }
        }

        public void AddContentRenderedHandler(StartHandlerType _dele)
        {
            StartHandlerChain.Add(_dele);
        }

        public void ResetContentRenderedHandler()
        {
            StartHandlerChain = null;
            StartHandlerChain = new List<StartHandlerType>();
        }
    }

}
