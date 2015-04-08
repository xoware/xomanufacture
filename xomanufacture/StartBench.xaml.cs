using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for StartBench.xaml
    /// </summary>
    public partial class StartBench : UserControl
   {

        private StartBenchViewModel MyViewModel;
        private bool InitOnceDone = false;
        
        public StartBench()
        {
            DataContextChanged += new DependencyPropertyChangedEventHandler(DC_Init);
            InitializeComponent();
        }

        private void DC_Init(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!InitOnceDone)
            {
                InitOnceDone = true;
                MyViewModel = this.DataContext as StartBenchViewModel;
                MyViewModel.ResetEventHandlerChain();
                MyViewModel.EnableEnterEvent += new PropertyChangedEventHandler(EnterAction);
            }
        }

        private void EnterAction(object sender, PropertyChangedEventArgs e)
        {
            var MyCommand = sender as String;
            if (MyCommand == "EnableEnter")
            {
                StartButton.IsEnabled = true;
                StartButton.Content = "Start Station";
                StartButton.CommandParameter = "Next";
            }
            if (MyCommand == "EnableExit")
            {
                StartButton.IsEnabled = true;
                StartButton.Content = "Exit Station";
                StartButton.CommandParameter = "Exit";
            }
        }
    }
}
