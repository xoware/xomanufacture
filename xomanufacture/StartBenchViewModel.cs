using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


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

            EnableStart();
        }
        public BarCodeScanner BCScanObject;


        private void EnableStart()
        {
            if (TheController.DoStartupChecks())
            {
                if (EnableEnterEvent != null)
                {
                    EnableEnterEvent("EnableEnter", new PropertyChangedEventArgs("nothing here"));
                }
            }
            else
            {
                if (EnableEnterEvent != null)
                {
                    EnableEnterEvent("EnableExit", new PropertyChangedEventArgs("nothing here"));
                }
            }
        }

    }
}
