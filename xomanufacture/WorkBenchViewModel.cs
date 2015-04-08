using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;


namespace xomanufacture
{
    class WorkBenchViewModel : aBenchViewModel
    {
 
       public WorkBenchViewModel(AController _controller) : base(_controller)
        {
            Name = "WorkBench";
            BCScanObject = new BarCodeScanner();
            ReflectUIStack = new List<ReflectUI>(new ReflectUI[16]);
        }

       public void StartPageSignal()
       {
           TheController.CtrlThread.Start();

       }

        public BarCodeScanner BCScanObject;
        private int TopIndex;
        public List<ReflectUI> ReflectUIStack;
        public event PropertyChangedEventHandler UpdateUIEvent;
        public void ResetEventHandlerChain()
        {
            this.UpdateUIEvent = null;
        }
        public String LabelBox;

       public ICommand ResetCommand
       {
           get
           {
               return new RelayCommand(NextOReset);
           }
       }
       private void NextOReset(object Parameter)
       {
           // call the object.functionns to walk the list, pick the next ready EN and
           TopIndex = TheController.PickNextUT();
           UpdateUI(" New ExoNet DUT ");

           // send shine_flasher by just setting ShinePending=true
           TheController.SetShinePending(TopIndex);
           // and wait 10 sec(per sec quanta) for blinkingstatus to enable scanning.
           int CoundDown = 10;
           while (CoundDown > 0)
           {
               if (TheController.ReturnBlinkingStatus(TopIndex))
               {
                   BCScanObject.FireEnableEvent(PostScanHook);
                   // enable the scan button.

                   break;
               }
               CoundDown--;
               Task.Factory.StartNew(() =>
               {
                   Thread.Sleep(1000); // this line won't make UI freeze.
               });
           }
           //Error if the execution gets here
       }

       public void PostScanHook(String ScanValue)
       {

           // NOTE: the main thread will 
           // after scanning
           // Update the barcode in the ExoNetUT object
           TheController.SaveBarCodeValue(TopIndex, ScanValue);
           // UIUPDATE will happen automaticcally.
           //but could call it here aswell
           // update the status and commitPersist(rundone.txt) of this EN to file
           String LabelData = TheController.GetLabel(TopIndex);
           //TODO
           // print label at the end of commiting 
       }

       public ICommand PauseCommand
       {
           get
           {
               return new RelayCommand(PauseAReprint);
           }
       }
       private void PauseAReprint(object Parameter)
       {
           //TODO
           //reprint last label here
           //get information about TopIndex and reprint the label
       }

       public void UpdateUI(String _status)
       {
           // transcode_copy all the changes from ExonetUIstack to Reflectuistack
           // also status String
           if (UpdateUIEvent != null)
           {
               for (int i = 0; i < 16; i++)
               {
                   if (TheController.ReturnLabeledStatus(i))
                   {
                       continue;
                   }
                   if (TheController.IsAlive(i))
                   {
                       ReflectUIStack[i].Light1 = Brushes.Red;
                       ReflectUIStack[i].Status = "Connected";
                   }
                   else
                   {
                       ReflectUIStack[i].Status = "Not Connected";
                       ReflectUIStack[i].Light1 = Brushes.Gray;
                       ReflectUIStack[i].Light2 = Brushes.Gray;
                       ReflectUIStack[i].Light3 = Brushes.Gray;
                       continue;
                   }
                   if (TheController.ReturnSvcdStatus(i))
                   {
                       ReflectUIStack[i].Light2 = Brushes.Red;
                       ReflectUIStack[i].Status = "Firmware Serviced";
                   }
                   if (TheController.ReturnReadyPending(i))
                   {
                       ReflectUIStack[i].Light3 = Brushes.Red;
                       ReflectUIStack[i].Status = "Alloted and Testing";
                   }
                   if (TheController.ReturnReadyPending(i) && TheController.ReturnPingStatus(i))
                   {                   
                       ReflectUIStack[i].Status = "Ready for Scanning";
                   }
                   if (i == TopIndex)
                   {
                       ReflectUIStack[i].Light1 = Brushes.Green;
                       ReflectUIStack[i].Light2 = Brushes.Green;
                       ReflectUIStack[i].Light3 = Brushes.Green;
                       LabelBox = "Scanning";
                       ReflectUIStack[i].Visibility = 1;
                       ReflectUIStack[i].Status = "Ready for Labeling";
                       if (TheController.ReturnBarCode(i) != "")
                       {
                           ReflectUIStack[i].Status = "Scanned";
                           LabelBox = "     BarCode Scanned : " + TheController.ReturnBarCode(i) + "     MAC : " + TheController.ReturnEtherMac(i);
                       }
                       if (TheController.ReturnBlinkingStatus(i))
                           ReflectUIStack[i].Status = "BLINKING : APPLY LABEL";
                   }
                   else
                   {
                       ReflectUIStack[i].Visibility = 0.3;
                   }
               }
               String StatString = LabelBox + "|" + _status;
               UpdateUIEvent(ReflectUIStack, new PropertyChangedEventArgs(StatString));
           }

       }
    }

    class ReflectUI
    {
        // all the updatable ui elements 
        public Brush Light1;
        public Brush Light2;
        public Brush Light3;
        public String Status;
        public int Visibility;
    }
}
