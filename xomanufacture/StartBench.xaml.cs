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

        private KeyEventHandler ScanPKDDelegate;
        private KeyEventHandler ScanPKUDelegate;
        
        public StartBench()
        {
            DataContextChanged += new DependencyPropertyChangedEventHandler(DC_Init);
            //Loaded += (ob, ev) => MyViewModel.StartPageFunc();
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

                MyViewModel.BCScanObject.ResetEventHandlerChain();
                MyViewModel.BCScanObject.ScanActionEvent += new PropertyChangedEventHandler(Scan_Action);
                ScanPKDDelegate = new KeyEventHandler(MyViewModel.BCScanObject.Scan_PreviewKeyDown);
                ScanPKUDelegate = new KeyEventHandler(MyViewModel.BCScanObject.Scan_PreviewKeyUp);
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
            if (MyCommand == "TestPrinter")
            {
                TestPrint.IsEnabled = true;
            }
            ConsoleLabel.Content = e.PropertyName;
        }

        private void Scan_Start(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += ScanPKDDelegate;
            this.PreviewKeyUp += ScanPKUDelegate;
            TestScan.Background = Brushes.Red;
	    ConsoleLabel.Content = "Please SCAN Printer_Test LABEL JUST PRINTED";
        }
        private void Scan_Action(object sender, PropertyChangedEventArgs e)
        {
            var ScanCom = sender as String;
            if (ScanCom == "StopScan")
            {
                this.PreviewKeyDown -= ScanPKDDelegate;
                this.PreviewKeyUp -= ScanPKUDelegate;
                TestScan.Background = Brushes.LightGray;
                TestScan.IsEnabled = false;
            }
            if (ScanCom == "EnableScan")
            {
                TestScan.IsEnabled = true;
            }
        }

    }
}
