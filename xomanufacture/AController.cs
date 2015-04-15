﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Threading;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Packets.Ethernet;
using System.Runtime.InteropServices;
using BRCLI;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NETWORKLIST;
using NetFwTypeLib;
using NETCONLib;
using System.Reflection;




namespace xomanufacture
{
    class AController : INotifyPropertyChanged
    {

        private static App ThisApp;
        private static MainWindow MyWindow;
        private static aBenchViewModel _nextPageViewModel;
        private static aBenchViewModel _currentPageViewModel;
        private static List<aBenchViewModel> _pageViewModels;
        private static AModel TheModel;

        public Thread CtrlThread;
        // 1 thread tied to processing the libpcap data, so updating all mac/ip 
        // 1 thread tied to controlling the  Opentftpd instance and updating list for that data
        // 1 thread tied to SignalProtocol to talk the the EnUT(s) 
        // these are descendents of the CtrlThread. 


        public static String BaseDir;

        private static void SetRunDate()
        {
            AModel.TodaysDate = DateTime.Now.ToString("yyMMdd");
            AModel.StartTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }
        private static void SetEndDate()
        {
            AModel.EndTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }
        public String GetRunDate()
        {
            return AModel.TodaysDate;
        }

        public AController(App _thisapp, MainWindow _appwindow)
        {

            ThisApp = _thisapp;
            MyWindow = _appwindow;
            TheModel = new AModel();
            BaseDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TheModel.PathName = BaseDir;
            SetRunDate();

            // Add available pages
            PageViewModels.Add(new StartBenchViewModel(this));
            PageViewModels.Add(new WorkBenchViewModel(this));

            CtrlThread = new Thread(new ThreadStart(CtrlThreadFunc));

            CtrlThread.IsBackground = false;
            //Dont call the start of all threads via start of controlthread here.
            //Starting it in the appropriate page via the page's startpagesignal
            //CtrlThread.Start();

            // Set starting page
            CurrentPageViewModel = PageViewModels[0];
            
        }

        public static void CtrlThreadFunc()
        {
            //instantiate  the other threads
            Thread PcapThread = new Thread(new ThreadStart(PcapThreadFunc));
            Thread TftpThread = new Thread(new ThreadStart(TftpdThreadFunc));
            Thread SignalPThread = new Thread(new ThreadStart(SignalPThreadFunc));
            //start the threads
            PcapThread.Start();
            TftpThread.Start();
            SignalPThread.Start();

            // keep saving in-flight by calling 
            // SaveToFile(inflight.txt)
            // within that loop
            while (true)
            {
                Ping Pong = new Ping();
                int timeout = 1;
                PingReply reply = Pong.Send("192.168.2.1", timeout);
                System.Threading.Thread.Sleep(2000);
                TheModel.SaveInFlight();
                //TODO
                //call to update the view.
                _currentPageViewModel.UpdateUI(TheModel.TodayStatus.ToString());
            }
            
        }

        public bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }
        public ConsoleToken DoStartupChecks()
        {
            ConsoleToken Reply = new ConsoleToken();
            Reply.Status = IsUserAdministrator();
            if (Reply.Status == false)
            {
                Reply.Message = "This Application Needs to be run as Administrator.";
                return Reply;
            }
            //put all the startup stuff here
            //read the configuration file for ethernet address guids and mac pool
            //load the pool list into TheModel and the adapters into the Acontroller objects respectively
            Reply.Status = TheModel.bootup();
            if (Reply.Status == false)
            {
                //this is a new installation. or lost conf file. user intervention required.
                Reply.Message = "this is a new installation. or lost conf file. user intervention required.";
                return Reply;
            }
            //firewall(windows ICS on the main(the internet) ethernet port)
            //also check to make sure that there are 2 adapters up?
            //also make sure that the windows firewall is turned off for the 2 ethernet ports
            Reply.Status = PrepNetworks();
            if (Reply.Status == false)
            {
                Reply.Message = "Could Not prep the networks.";
                return Reply;
            }

            //check to make sure winpcap is installed if that can be done.
            if (!File.Exists(@"C:\Program Files\WinPcap\install.log"))
            {
                Reply.Status = false;
                Reply.Message = "WinPCAP installation not found. Please make sure its installed in default location. Also that it startsup automatically at bootup.";
                return Reply;
            }
            // check pscp(putty) executable is in this directory also. // I could add it as a resource file.
            if (!File.Exists(TheModel.PathName + @"\pscp.exe"))
            {
                Reply.Status = false;
                Reply.Message = "Missing pscp.exe in the Application Dir";
                return Reply;
            }
            // If the reply of this function is false then print the reason in a textarea in the app.

            Reply.Message = " Start Checks Successful.  ";
            Reply.Status = true;
            return Reply;
        }
        private bool PrepNetworks()
        {
            bool reply1 = false;
            bool reply2 = false;
            bool reply = false;
            var someException = new System.Exception();
            String SharedAdap = "";

            foreach (var nic in IcsManager.GetIPv4EthernetAndWirelessInterfaces())
            {
                if (nic.Id == AModel.Adapter1 || nic.Id == AModel.Adapter2)
                {
                    if (nic.OperationalStatus == OperationalStatus.Up)
                    {
                        if (nic.Id == AModel.Adapter1)
                            reply1 = true;
                        if (nic.Id == AModel.Adapter2)
                            reply2 = true;

                        try
                        {
                            String netResp = IdToNetworkPrivatise(nic.Id);
                            if (netResp == "No Network")
                                throw someException;
                        }
                        catch
                        {
                            if (reply1)
                                reply1 = false;
                            if (reply2)
                                reply2 = false;
                        }
                    }
                    else
                    {
                        // this adapter is down????
                    }
                }
                else
                {
                    //found Shared/Internet network
                    SharedAdap = nic.Id;
                }
            }
            if (reply1 && reply2 && SharedAdap != "")
                EnableICS(SharedAdap, AModel.Adapter1, true);
            return reply1 && reply2;
        }
        public static String IdToNetworkPrivatise(String AdapID)
        {
            var NLM = new NetworkListManager();
            var NetList = NLM.GetNetworkConnections();
            String ReturnMesg = " ";
            foreach (INetworkConnection NetIter in NetList)
            {
                if (NetIter.GetAdapterId().ToString().ToUpper() == AdapID.Trim().TrimEnd('}').TrimStart('{'))
                {
                    ReturnMesg = NetIter.GetNetwork().GetName().ToString();
                    ReturnMesg += " : ";
                    ReturnMesg += NetIter.GetNetwork().GetCategory().ToString();
                    // set network private
                    NetIter.GetNetwork().SetCategory(NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PRIVATE);
                    // authorize this app to fw;   fallback measure
                    FirewallHelper.Instance.GrantAuthorization(Assembly.GetExecutingAssembly().Location,
                        "xomanufacture", NET_FW_SCOPE_.NET_FW_SCOPE_ALL, NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);
                    // disable the fw in private profiles
                    FirewallHelper.Instance.SetFirewallStatus(false);
                    //Console.WriteLine(FirewallHelper.Instance.HasAuthorization(Assembly.GetExecutingAssembly().Location).ToString());
                }
            }
            ReturnMesg = "No Network";
            return ReturnMesg;
        }
        //shared and home are in the format : nic.id
        static void EnableICS(string shared, string home, bool force)
        {
            var connectionToShare = IcsManager.FindConnectionByIdOrName(shared);
            if (connectionToShare == null)
            {
                Console.WriteLine("Connection not found: {0}", shared);
                return;
            }
            var homeConnection = IcsManager.FindConnectionByIdOrName(home);
            if (homeConnection == null)
            {
                Console.WriteLine("Connection not found: {0}", home);
                return;
            }

            var currentShare = IcsManager.GetCurrentlySharedConnections();
            if (currentShare.Exists)
            {
                Console.WriteLine("Internet Connection Sharing is already enabled:");
                Console.WriteLine(currentShare);
                if (!force)
                {
                    Console.WriteLine("Please disable it if you want to configure sharing for other connections");
                    return;
                }
                Console.WriteLine("Sharing will be disabled first.");
            }

            IcsManager.ShareConnection(connectionToShare, homeConnection);
        }



        private static void Intf1Handler(Packet packet)
        {
            List<ExoNetUT> Lref = TheModel.ExoNetStack;
            String SrcIp = packet.Ethernet.IpV4.Source.ToString();
            String SrcMac = packet.Ethernet.Source.ToString();

            bool foundit = false; 
            foreach (ExoNetUT myut in Lref) {
                if (myut.DynamicMac == SrcMac)
                {
                    myut.LinkLocalIP = SrcIp;
                    foundit = true;
                }
                if (myut.EtherMac1 == SrcMac)
                {
                    myut.DynamicIP = SrcIp;
                    foundit = true;
                }
            }
            if (!foundit)
            {
                //we can test if the srcmac falls out of the coded mac range in nor.img and
                //srcip is not in 169.254.254.0/24 range then we cant proceed with the following addition.
                //or should we?? (overlook the error condition??)
                //if we want to overlook then we just test if it is in the range of allocation pool and 
                //do the addition to the DynamicIP,EtherMac1.
                ExoNetUT NewUT = TheModel.ExoNetStack[TheModel.NewSlot()];
                NewUT.DynamicMac = SrcMac;
                NewUT.LinkLocalIP = SrcIp;
            }
        }
        private static void Intf2Handler(Packet packet)
        {
            List<ExoNetUT> Lref = TheModel.ExoNetStack;
            String SrcMac = packet.Ethernet.Source.ToString();

            bool foundit = false;
            foreach (ExoNetUT myut in Lref)
            {
                if (myut.EtherMac2 == SrcMac)
                {
                    myut.PingTestStatus = true;
                    foundit = true;
                }
            }
            if (!foundit)
            {
                //TODO
                //do something for the error condition.
            }

        }
        public static void PcapThreadFunc()
        {
            //libpcap object instantiate and while loop to process its data
            //basically infinitely snoop and update data
            //snoop on both interfaces and snooping for:
            // macs and ips associated with them
            // ping responses only from 2nd interface and the mac it came from.
            String Intf1BPF = "udp and dst 169.254.254.254 and (dst port 69 or dst port 36969)";
            String Intf2BPF = "icmp and src 192.168.2.1 and destination 192.168.2.254";
            //libpcap object device.name is contains our adapter1 guid.
            libpcapObj FirIntfProc = new libpcapObj(AModel.Adapter1, Intf1Handler, Intf1BPF);
            libpcapObj SecIntfProc = new libpcapObj(AModel.Adapter2, Intf2Handler, Intf2BPF);
            Thread Intf1Thread = new Thread(new ThreadStart(FirIntfProc.StartCapture));
            Thread Intf2Thread = new Thread(new ThreadStart(SecIntfProc.StartCapture));
            Intf1Thread.Start();
            Intf2Thread.Start();
            Intf1Thread.Join();
            Intf2Thread.Join();

        }



        public static void AddListFunc(IntPtr _ptrarg)
        {
            //Console.WriteLine("Running the AddListFunc");
            //Console.WriteLine(_ptrarg.ToString("X8"));

            List<ExoNetUT> Lref = TheModel.ExoNetStack;
            String AddToList = Marshal.PtrToStringAnsi(_ptrarg);

            bool foundit = false;
            foreach (ExoNetUT myut in Lref)
            {
                if (AddToList.Contains(myut.LinkLocalIP))  //also add that it doesnt contain failure message(?necessary?)
                {
                    myut.BootPhase = 0;
                }
            }
            if (!foundit)
            {
                //TODO
                //do something for the error condition.
            }
        }
        public static void TftpdThreadFunc()
        {
            //instaniate the opentftpdobj and process that data
            //basically infinitely serve and update data
            // serve the firmware and collect ip addresses and macs and update the
            // exonetut  corresponding objects within the list
            
            BRCLI.BridgeCall CallBackFunc = new BRCLI.BridgeCall(AddListFunc);

            BRCLI.BridgeC.RunCLI(CallBackFunc);

        }



        public static String ObtainMacRes()
        {
             
            String MacRes = "";
            if (TheModel.MacPoolList[0].IsExhausted())
            {
                TheModel.MacPoolList.RemoveAt(0);
                if (TheModel.MacPoolList.Count() == 0)
                {
                    //we have an error condition. all macs have exhausted midship.
                }
            }
            MacRes = TheModel.MacPoolList[0].NextAvailable.ToString();
            PhyMacAddr.increment(TheModel.MacPoolList[0].NextAvailable);
            return MacRes;

        }
        public static String GenerateSerialNumber()
        {
            // generate serial number by using date and dailycount.
            //get dailycount from TheModel.
            //this gets generated completely from date,time and dailyruncount
            int i = TheModel.TodayStatus.DayRunCount + 10000;

            String NewNumber = "5-";
            NewNumber += AModel.TodaysDate;
            NewNumber += i.ToString();

            return NewNumber;
        }
        public static String SPLogic(String RcvdData, String RemoteClient)
        {
            // do some work
            String ResponseData = "NoResponse";
            if (RcvdData.Contains("SvcReq"))
            {
                //find the ExoNetUT object
                foreach (ExoNetUT IterUT in TheModel.ExoNetStack)
                {
                    if (IterUT.LinkLocalIP == RemoteClient)
                    {
                        String MySrNumber = GenerateSerialNumber();
                        String MyMacNumber1 = ObtainMacRes();
                        String MyMacNumber2 = ObtainMacRes();

                        ResponseData = "ManufSR#" + MySrNumber + "|" +
                                        "ManufMAC1#" + MyMacNumber1 + "|" +
                                        "ManufMAC2#" + MyMacNumber2;
                        IterUT.EtherMac1 = MyMacNumber1;
                        IterUT.EtherMac2 = MyMacNumber2;
                        IterUT.SerialNo = MySrNumber;
                        IterUT.SvcdStatus = true;
                        TheModel.SaveInFlight();
                        break;
                    }                
                }
            }
            if (RcvdData.Contains("Ready"))
            {
                foreach (ExoNetUT IterUT in TheModel.ExoNetStack)
                {
                    if (IterUT.DynamicIP == RemoteClient)
                    {
                        IterUT.ReadyPending = true;
                        if (IterUT.ShinePending == true)
                        {
                            ResponseData = "ShineFlasher";
                        }
                        break;
                    }
                }
            }
            if (RcvdData.Contains("Blinking"))
            {
                foreach (ExoNetUT IterUT in TheModel.ExoNetStack)
                {
                    if (IterUT.DynamicIP == RemoteClient)
                    {
                        IterUT.BlinkingStatus = true;
                        break;
                    }
                }
            }
            return ResponseData;
        }
        public static void SignalPThreadFunc()
        {
            //instantiate signalprotocol object and use it talk to En(s)
            DelegateType SPDelegate = new DelegateType(SPLogic);
            SignalProtocol SPObj = new SignalProtocol();
            SPObj.DoWork(SPDelegate);
        }



//TODO
// UPDATE THE PICTURE of ENs at THE END OF EACH USER INTERACTION

        public void ChangeThePage()
        {
            if (_nextPageViewModel == null)
            {
                if (PageViewModels.Count > 1)
                {
                    if (PageViewModels.IndexOf(_currentPageViewModel) == (PageViewModels.Count - 1))
                    {
                        _nextPageViewModel = PageViewModels[0];
                    }
                    else
                    {
                        _nextPageViewModel = PageViewModels[PageViewModels.IndexOf(_currentPageViewModel)+1];
                    }
                }
                else
                {
                    _nextPageViewModel = _currentPageViewModel;
                }
            }
            ChangeViewModel(_nextPageViewModel);
        }
        public List<aBenchViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                    _pageViewModels = new List<aBenchViewModel>();

                return _pageViewModels;
            }
        }
        public aBenchViewModel CurrentPageViewModel
        {
            get
            {
                return _currentPageViewModel;
            }
            set
            {
                if (_currentPageViewModel != value)
                {
                    _nextPageViewModel = _currentPageViewModel;
                    _currentPageViewModel = value;
                    //firing off the change the page event for UI to actually do it.
                    NotifyPropertyChanged("CurrentPageViewModel");
                    _currentPageViewModel.StartPageSignal();
                }
            }
        }
        private void ChangeViewModel(aBenchViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            CurrentPageViewModel = PageViewModels
                .FirstOrDefault(vm => vm == viewModel);
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void ShutMeDown()
        {
            //somestuff
            TheModel.UploadLog();
            App.Current.Shutdown();
        }

        public int PickNextUT()
        {
            return TheModel.TopDUT();
        }

        public void SetShinePending(int UnitIndex)
        {
            TheModel.ExoNetStack[UnitIndex].ShinePending = true;
        }

        public bool ReturnBlinkingStatus(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].BlinkingStatus;
        }

        public bool IsAlive(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].Alive;
        }

        public bool ReturnPingStatus(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].PingTestStatus;
        }

        public bool ReturnLabeledStatus(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].LabeledStatus;
        }

        public bool ReturnSvcdStatus(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].SvcdStatus;
        }

        public bool ReturnReadyPending(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].ReadyPending;
        }

        public String ReturnBarCode(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].BarcodeData;
        }

        public String ReturnEtherMac(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].EtherMac1;
        }

        public String ReturnSerialNo(int UnitIndex)
        {
            return TheModel.ExoNetStack[UnitIndex].SerialNo;
        }

        public void SaveBarCodeValue(int TopIndex, string ScanValue)
        {
            TheModel.ExoNetStack[TopIndex].BarcodeData = ScanValue;
        }

        public String GetLabel(int TopIndex)
        {
            TheModel.CommitDone(TopIndex);
            String Response = "|";
            Response += TheModel.ExoNetStack[TopIndex].EtherMac1 + "|";
            Response += TheModel.ExoNetStack[TopIndex].SerialNo + "|";
            return Response;
        }
    }



    class aBenchViewModel
    {
        public static String Name;
        protected AController TheController;

        public aBenchViewModel(AController _controller)
        {
            TheController = _controller;
        }

        public void StartPageSignal() {}

        public void UpdateUI(String _stat) {}
        
        public ICommand ChangePageCommand
        {
            get
            {
                return new RelayCommand(ChangePage);
            }
        }

        private void ChangePage(object _parameter)
        {
            String Parameter = _parameter as String;
            if (Parameter == "Next")
            {
                this.TheController.ChangeThePage();
            }
            if (Parameter == "Exit")
            {
                this.TheController.ShutMeDown();
            } 
        }
    }

    public delegate void Action(object param);
    public class RelayCommand : ICommand
    {
        private readonly Action _action;

        public RelayCommand(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}