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
    //delegate void ScanDelegateType(object sender, KeyEventArgs e);

    /// <summary>
    /// Interaction logic for WorkBench.xaml
    /// </summary>
    public partial class WorkBench : UserControl
    {
        private WorkBenchViewModel MyViewModel;
        private KeyEventHandler ScanPKDDelegate;
        private KeyEventHandler ScanPKUDelegate;
        private bool InitOnceDone = false;

        private StackPanel[] StackPanelArr;
        private ENUTPanelPointer[] DUTStackPanel;


        public WorkBench()
        {

            DataContextChanged += new DependencyPropertyChangedEventHandler(DC_Init);
            //Loaded += (ob, ev) => MyViewModel.StartPageFunc();
            InitializeComponent();
            StackPanelArr = new[] {ENUT00, ENUT01, ENUT02, ENUT03, ENUT04, ENUT05, ENUT06, ENUT07, ENUT08, ENUT09, ENUT10, ENUT11, ENUT12, ENUT13, ENUT14, ENUT15};
            DUTStackPanel = new ENUTPanelPointer[16];
            for (int i = 0; i < 16; i++)
            {
		DUTStackPanel[i] = new ENUTPanelPointer();
                DUTStackPanel[i].Light1 = StackPanelArr[i].Children[0] as Ellipse ;
                DUTStackPanel[i].Light2 = StackPanelArr[i].Children[1] as Ellipse;
                DUTStackPanel[i].Light3 = StackPanelArr[i].Children[2] as Ellipse;
                DUTStackPanel[i].Status = StackPanelArr[i].Children[3] as TextBlock;
                DUTStackPanel[i].ENBox = DUTViewStack.Children[i] as GroupBox;
            }
            for (int i = 0; i < 16; i++)
            {
                DUTStackPanel[i].Light1.Fill = Brushes.Gray;
                DUTStackPanel[i].Light2.Fill = Brushes.Gray;
                DUTStackPanel[i].Light3.Fill = Brushes.Gray;
                DUTStackPanel[i].Status.Text = "Not Connected";
                DUTStackPanel[i].ENBox.Opacity = 0.3;
            }

        }


        private void DC_Init(object sender, DependencyPropertyChangedEventArgs e)
        {
            // One can also validate the data going into the DataContext using the event args
            if (!InitOnceDone)
            {
                InitOnceDone = true;
                MyViewModel = this.DataContext as WorkBenchViewModel;

                MyViewModel.BCScanObject.ResetEventHandlerChain();
                MyViewModel.BCScanObject.ScanActionEvent += new PropertyChangedEventHandler(Scan_Action);
                ScanPKDDelegate = new KeyEventHandler(MyViewModel.BCScanObject.Scan_PreviewKeyDown);
                ScanPKUDelegate = new KeyEventHandler(MyViewModel.BCScanObject.Scan_PreviewKeyUp);

                MyViewModel.ResetEventHandlerChain();
                MyViewModel.UpdateUIEvent += new PropertyChangedEventHandler(ReflectChanges);
            }
        }


        private void ActivateScanAcquire()
        {
            this.PreviewKeyDown += ScanPKDDelegate;
            this.PreviewKeyUp += ScanPKUDelegate;
        }

        private void DeactivateScanAcquire()
        {
            this.PreviewKeyDown -= ScanPKDDelegate;
            this.PreviewKeyUp -= ScanPKUDelegate;
        }

        private void Scan_Start(object sender, RoutedEventArgs e)
        {
            ActivateScanAcquire();
            ScanButton.Background = Brushes.Red;
        }
        private void Scan_Action(object sender, PropertyChangedEventArgs e)
        {
            var ScanCom = sender as String;
            if (ScanCom == "StopScan")
            {
                DeactivateScanAcquire();
                ScanButton.Background = Brushes.LightGray;
                ScanButton.IsEnabled = false;
                WorkBox.Text = e.PropertyName;
                //MessageBox.Show(e.PropertyName);
            }
            if (ScanCom == "EnableScan")
            {
                ScanButton.IsEnabled = true;
            }
        }

        private void ReflectChanges(object sender, PropertyChangedEventArgs e)
        {
            //Routine update the property elements
            List<ReflectUI> Reflection = sender as List<ReflectUI>;
            for (int i = 0; i < 16; i++)
            {
                //update all elements of all en visuals
                DUTStackPanel[i].Light1.Fill = Reflection[i].Light1;
                DUTStackPanel[i].Light2.Fill = Reflection[i].Light2;
                DUTStackPanel[i].Light3.Fill = Reflection[i].Light3;
                DUTStackPanel[i].Status.Text = Reflection[i].Status;
                DUTStackPanel[i].ENBox.Opacity = Reflection[i].Visibility;
            }
            WorkBox.Text = e.PropertyName;
        }
    }
    class ENUTPanelPointer
    {
        public Ellipse Light1;
        public Ellipse Light2;
        public Ellipse Light3;
        public TextBlock Status;
        public GroupBox ENBox; 
    }
}
