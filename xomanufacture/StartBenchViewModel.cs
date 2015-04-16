using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;


namespace xomanufacture
{
    class StartBenchViewModel : aBenchViewModel
    {
        public event PropertyChangedEventHandler EnableEnterEvent;

        public void ResetEventHandlerChain()
        {
            this.EnableEnterEvent = null;
        }

        public StartBenchViewModel(AController _controller) : base(_controller)
        {
            Name = "StartBench";
            BCScanObject = new BarCodeScanner();

        }
        public BarCodeScanner BCScanObject;
        public String TestIOToken;

        public override void StartPageFunc()
        {
            var response = TheController.DoStartupChecks();
            if (response.Status)
            {
                if (EnableEnterEvent != null)
                {
                    EnableEnterEvent("TestPrinter", new PropertyChangedEventArgs("Click  [Test Printer] to continue"));
                }
            }
            else
            {
                if (EnableEnterEvent != null)
                {
                    EnableEnterEvent("EnableExit", new PropertyChangedEventArgs(response.Message));
                }
            }
        }


        public ICommand TestPrintCommand
        {
            get
            {
                return new RelayCommand(ClickedPrintAction);
            }
        }

        private void ClickedPrintAction(object _parameter)
        {
            // do the printing and then: Save the token
            TestIOToken = TheController.GetRunDate();
            LabelPrinter.PrintLabel(TestIOToken, TestIOToken);
            BCScanObject.FireEnableEvent(PostScanHook);
	    // update the message
            EnableEnterEvent("UpdtMsg", new PropertyChangedEventArgs("Click  [Test Scanner] to Continue"));
        }
        public void PostScanHook(String ScanValue)
        {
            //check the token.
            if (ScanValue == TestIOToken)
                EnableEnterEvent("EnableEnter", new PropertyChangedEventArgs("Click  [Start Station] to Continue"));
            else
                EnableEnterEvent("EnableExit", new PropertyChangedEventArgs("Try Again OR Exit"));
        }

    }
}
