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
            for (int index = 0; index < 16; index++)
            {
                ReflectUIStack[index] = new ReflectUI();
            }
            TopIndex = -1;
        }

       public void StartPageLoad()
       {
           TheController.CtrlThread.Start();

       }

        public BarCodeScanner BCScanObject;
        private int TopIndex;
        private bool ScanEnabled;
        public List<ReflectUI> ReflectUIStack;
        public event PropertyChangedEventHandler UpdateUIEvent;
        public void ResetEventHandlerChain()
        {
            this.UpdateUIEvent = null;
        }
        public String LastLabel;

       public ICommand ResetCommand
       {
           get
           {
               return new RelayCommand(DoneNext);
           }
       }
       private void DoneNext(object Parameter)
       {
           if (TopIndex != -1)
           {
               // order is important first clear label, then clear alive and giveup index
               TheController.ClearLabeledStatus(TopIndex);
               TheController.ReCycle(TopIndex);
               TopIndex = -1;
           }
       }
       private String NextOReset()
       {
           String LabelBx;
           // call the object.functionns to walk the list, pick the next ready EN and
           TopIndex = TheController.PickNextUT();
           if (TopIndex == -1)
               return "";

           LabelBx = Environment.NewLine;
           LabelBx += Environment.NewLine;
           LabelBx += "====== Preparing New Available XoNet DUT ======";
           LabelBx += Environment.NewLine;
           LabelBx += "------> Please wait several seconds... <------";
           LabelBx += Environment.NewLine;
           LabelBx += Environment.NewLine;

           // send shine_flasher by just setting ShinePending=true
           TheController.SetShinePending(TopIndex);
           ScanEnabled = false;
           return LabelBx;
       }

       public void PostScanHook(String ScanValue)
       {
           char[] delim = {'|'};
           // NOTE: the main thread will 
           // after scanning
           // Update the barcode in the ExoNetUT object
           TheController.SaveBarCodeValue(TopIndex, ScanValue);
           //Update the UI: Necessary to do this here.
           StatusLabelCombo LabelStatus = new StatusLabelCombo();
           LabelStatus.LabelBox = "";
           LabelStatus.Status = "";
           UpdateUI(LabelStatus);

           // update the status and commitPersist(rundone.txt) of this EN to file
           LastLabel = TheController.GetLabel(TopIndex).Trim(delim);
           // print label at the end of commiting 
           LabelPrinter.PrintLabel(
               LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[0], 
               LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[1]
               );
           LabelPrinter.PrintLabel(
                LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[0],
                LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[1]
                );
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
           //reprint last label here
           //get information about TopIndex and reprint the label
           char[] delim = { '|' };
           LabelPrinter.PrintLabel(
                LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[0],
                LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[1]
                );
           LabelPrinter.PrintLabel(
                LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[0],
                LastLabel.Split(delim, StringSplitOptions.RemoveEmptyEntries)[1]
                );
       }

       public override void UpdateUI(StatusLabelCombo LabelStatus)
       {
           String LabelBx = "";
           if (TopIndex == -1)
           {
               LabelBx = NextOReset();
           }

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
                       ReflectUIStack[i].Light2 = Brushes.Gray;
                       ReflectUIStack[i].Light3 = Brushes.Gray;
                       ReflectUIStack[i].Status = "Connected: ";
                       ReflectUIStack[i].Status += TheController.ReturnLinkLocalIP(i);
                   }
                   else
                   {
                       ReflectUIStack[i].Status = "Not Connected";
                       ReflectUIStack[i].Light1 = Brushes.Gray;
                       ReflectUIStack[i].Light2 = Brushes.Gray;
                       ReflectUIStack[i].Light3 = Brushes.Gray;
                       ReflectUIStack[i].Visibility = 0.5;
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
                       ReflectUIStack[i].Visibility = 1;

                       if (TheController.ReturnBlinkingStatus(TopIndex) && !ScanEnabled)
                       {
                           LabelBx += "====== New XoNet DUT *BLINKING* *BLINKING* ======";
                           UIScanEnable();
                           // enable the scan button.
                           LabelBx += Environment.NewLine;
                           //LabelBx += "====== *BLINKING* *BLINKING* ======";
                           //LabelBx += Environment.NewLine;
                           LabelBx += Environment.NewLine;
                           LabelBx += Environment.NewLine;

                           LabelBx += "------> IDENTIFY THE CONTINUOUSLY BLINKING BOARD <------";
                           LabelBx += Environment.NewLine;
                           LabelBx += "------> CLICK [START SCAN] TO CONTINUE <------";
                           LabelBx += Environment.NewLine;
                           LabelBx += "then use scanner to scan the pcb barcode of the blinking board";
                           LabelBx += Environment.NewLine;
                           LabelBx += Environment.NewLine;
                           //LabelBx += Environment.NewLine;
                       }

                       if (TheController.ReturnBarCode(i) != "")
                       {
                           ReflectUIStack[i].Status = "Scanned";
                           LabelBx += "PCB Scanned : " + TheController.ReturnBarCode(i) ;
                           LabelBx += Environment.NewLine;
                           LabelBx += "MAC : " + TheController.ReturnEtherMac(i);
                           LabelBx += Environment.NewLine;
                           LabelBx += "Label Printed";
                           LabelBx += Environment.NewLine;
                           LabelBx += Environment.NewLine;
                           LabelBx += Environment.NewLine;
                           LabelBx += "------> DISCONNECT PCB & APPLY LABEL <------";
                           LabelBx += Environment.NewLine;
                           LabelBx += "------> WHEN DONE CLICK [NEXT] TO CONTINUE <------";
                           LabelBx += Environment.NewLine;
                           LabelBx += "Caution:  DO NOT click NEXT before disconnecting the PCB";
                       }

                       if (TheController.ReturnBlinkingStatus(i))
                       {
                           ReflectUIStack[i].Status = "BLINKING";
                       }
                   }
                   else
                   {
                       ReflectUIStack[i].Visibility = 0.5;
                   }
               }
               LabelStatus.LabelBox += LabelBx;

               UpdateUIEvent(ReflectUIStack, new PropertyChangedEventArgs(LabelStatus.ToString()));
           }
       }
       private void UIScanEnable()
       {
           //Dispatcher.CurrentDispatcher.Invoke(() =>
           // DONT WANT CURRENT DISPATCHER!!! App.Current is UI we will make double
           // sure of this in the code-behind when handling this event.
           App.Current.Dispatcher.Invoke(() =>
           {
               ScanEnabled = true;
               BCScanObject.FireEnableEvent(PostScanHook);
           });
       }
    }

    class ReflectUI
    {
        // all the updatable ui elements 
        public Brush Light1;
        public Brush Light2;
        public Brush Light3;
        public String Status;
        public Double Visibility;
    }
}
