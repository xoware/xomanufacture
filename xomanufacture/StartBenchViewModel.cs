using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;


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
            // since this is not long running, should do either of these modern methods 
            //  instead of thread or threadpool/background worker
            // either plain tasks or delegate_dispatcher/begin_invoke
            //var child = Task.Factory.StartNew(() => 
            //since there is an issue of thread changing so can only use
            //dispatcher with a delegate/closure.
            tempdeletype adel = StartPageFuncDele;
            //App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, adel);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, adel);

        }
        public delegate void tempdeletype();
        public void StartPageFuncDele()
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
            int i = 10000;
            TestIOToken = "5-";
            TestIOToken += TheController.GetRunDate();
            TestIOToken += i.ToString();
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
