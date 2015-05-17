using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System.Runtime.InteropServices;
using BRCLI;
using System.Net.Sockets;
using System.Net;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Base;
using System.IO;
using System.Windows.Input;
using System.ComponentModel;
using System.IO.Compression;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Windows;
using Zen.Barcode;
using System.Windows.Markup;


namespace xomanufacture
{

    class AModel
    {
        private Object CriticalSection;
        //use this CriticalSection to lock the birth and death of ExoNetUT datastructes in a slot
        // to enter the CS use a lock block: lock(CriticalSection) { //this is CS }

        public List<ExoNetUT> ExoNetStack;
        public List<MacAddPool> MacPoolList;
        public StatusBar TodayStatus;

        public static String TodaysDate;
        public static String StartTime;
        public static String EndTime;
        public String StationName;

        public AModel() 
        {
            CriticalSection = new Object();
            ExoNetStack = new List<ExoNetUT>(new ExoNetUT[16]);
            for (int index = 0; index < ExoNetStack.Count; index++)
            {
                ExoNetStack[index] = new ExoNetUT();
            }
            MacPoolList = new List<MacAddPool>();
            // ??load some junk values in this incase of non-manuf uses of this software
            MyLock = new Object();
            TodayStatus = new StatusBar();
            StationName = "abc";
        }

        private void SaveToFile(String FileName)
        {
            //Iterate through the entire Model and for each 
            //instance of EN ToString and save to file
            //add delimiter--------------------to file
            // also save MacPoolList currently in memory tostring and save that to file
            //add delimiter--------------------to file
            // and just dump today status in memory tostring and save that to file
            //File.Create(FileName);
            File.AppendAllText(FileName, "InFlightFileSection:     MacPoolList" + Environment.NewLine);
            foreach (MacAddPool IterMAC in MacPoolList)
            {
                File.AppendAllText(FileName, IterMAC.ToString() + Environment.NewLine);
            }

            File.AppendAllText(FileName, "InFlightFileSection:     TodayStatus" + Environment.NewLine);
            File.AppendAllText(FileName, TodayStatus.ToString() + Environment.NewLine);

            File.AppendAllText(FileName, "InFlightFileSection:     ExoNetStack" + Environment.NewLine);
            foreach (ExoNetUT IterUT in ExoNetStack)
            {
                File.AppendAllText(FileName, IterUT.ToString() + Environment.NewLine);
            }
             
        }

        private void LoadFromFile(String FileName)
        {
            // Read one line at a time and load to the stack_list by calling fromstring ctor
            // catch delimiter and
            // load macpoollist from next full line strings read from file. using fromstring ctor
            // catch delimiter and
            // load today status into memory from a line string.
            MacPoolList = new List<MacAddPool>();
            String CurrentSection = " ";
            if (File.Exists(FileName))
            {
                foreach (string line in File.ReadLines(FileName))
                {
                    if (line.Contains("InFlightFileSection:") & line.Contains("MacPoolList"))
                    {
                        CurrentSection = "MacPoolList";
                    }
                    else if (line.Contains("InFlightFileSection:") & line.Contains("TodayStatus"))
                    {
                        CurrentSection = "TodayStatus";
                    }
                    else if (line.Contains("InFlightFileSection:") & line.Contains("ExoNetStack"))
                    {
                        CurrentSection = "ExoNetStack";
                    }
                    else if (CurrentSection == "MacPoolList")
                    {
                        MacPoolList.Add(new MacAddPool(line));
                    }
                    else if (CurrentSection == "TodayStatus")
                    {
                        TodayStatus = new StatusBar(line);
                    }
                    else if (CurrentSection == "ExoNetStack")
                    {
                        ExoNetStack[NewSlot()] = new ExoNetUT(line);
                    }
                }
            }
        }

        public int TopDUT()
        {
            foreach (ExoNetUT IterUT in ExoNetStack)
            {
                IterUT.LabeledStatus = false;
            }
            // walk the queue and find the next ready EN and 
            // (a ready en is one that has passed the teststatus and readypending==true)
            //return its index
            int retindex = -1;
            for (int index = 0; index < ExoNetStack.Count; index++)
            {
                if (ExoNetStack[index].Alive == true)
                {
                    if (ExoNetStack[index].PingTestStatus == true && ExoNetStack[index].ReadyPending == true)
                    {
                        retindex = index;
                        break;
                    }
                }
            }
            return retindex;
        }

        //Make a slot dead
        private void CommitPersist(string FileName, int index)
        {
            // Take the  EN at list[index] and convert tostring and save to 
            // Filename 
            // change its Alive to false;
            File.AppendAllText(FileName, ExoNetStack[index].ToString() + Environment.NewLine);
            lock (CriticalSection)
            {
                //ExoNetStack[index].Alive = false;
                //dont kill here but after the next is pressed. since 
                //it becomes a ghost alive if recycled here, since its still blinking
                //moving this to DoneNext(...)
                ExoNetStack[index].LabeledStatus = true;
            }
            TodayStatus.DayRunCount++;
        }

        //Make a slot Alive
        public int NewSlot()
        {
            // walk the list and return the index of the first dut that is done.
            // make it alive; Alive = true;
            // return the index
            int index = 0;
            foreach (ExoNetUT IterUT in ExoNetStack)
            {
                if (IterUT.Alive != true)
                {
                    index = ExoNetStack.IndexOf(IterUT);
                    break;
                }
            }
            lock (CriticalSection)
            {
                ExoNetStack[index].Alive = true;
                ExoNetStack[index].DynamicMac = "";
                ExoNetStack[index].EtherMac1 = "";
                ExoNetStack[index].EtherMac2 = "";
                ExoNetStack[index].DynamicIP = "";
                ExoNetStack[index].LinkLocalIP = "";
                ExoNetStack[index].SerialNo = "";
                ExoNetStack[index].BarcodeData = "";
                ExoNetStack[index].BootPhase = -5;
                ExoNetStack[index].PingTestStatus = false;
                ExoNetStack[index].BlinkingStatus = false;
                ExoNetStack[index].SvcdStatus = false;
                ExoNetStack[index].ReadyPending = false;
                ExoNetStack[index].ShinePending = false;
                ExoNetStack[index].LabeledStatus = false;
            }
            return index;
        }


        public static String Adapter1;
        public static String Adapter2;

        public String PathName;
        private Object MyLock;

        public void SaveConFile()
        {
            // save values of adapter1
            // save values of adapter2
            // save macpoollist
            Object SaveLock = new Object();
            lock (SaveLock)
            {
                String coname = PathName + @"\xomanuf.conf";
                File.Delete(coname);
                File.AppendAllText(coname, "ADAPTER1#" + Adapter1 + Environment.NewLine);
                File.AppendAllText(coname, "ADAPTER2#" + Adapter2 + Environment.NewLine);
                for (int index = 0; index < MacPoolList.Count; index++)
                {
                    MacAddPool IterMAC = MacPoolList[index];
                    File.AppendAllText(coname, "MACPOOL" + index.ToString() + "=" + IterMAC.ToString() + Environment.NewLine);
                }
            }
        }

        public bool LoadConFile()
        {
            //check for xomanuf.conf : contains only 2 lines with adapter guid and macpools
            //                              adapter1#...
            //                              adapter2#...
            //                              macpool1#aa:bb:cc:dd:ee:00-aa:bb:cc:dd:ee:ff
            //                              macpooln#....
            // if it exists and if the values are good(nonmarker)
            // load values of adapter1
            // load values of adapter2
            // read in each line of macpooln  
            //          create a new String line.Substring(IndexOf("=") + 1);
            // Reload macpoollist
            // else
            // create default file with marker values
            String coname = PathName + @"\xomanuf.conf";
            bool shell = false;
            if (File.Exists(coname))
            {
                foreach (String FileLine in File.ReadAllLines(coname))
                {
                    if (FileLine.Contains("marker values replace with correct"))
                    {
                        shell = true;
                    }
                }
            }
            else
            {
                File.AppendAllText(coname, "ADAPTER1#marker values replace with correct :GUID eg:{BBD7D6C7-4475-44B7...}" + Environment.NewLine);
                File.AppendAllText(coname, "ADAPTER2#marker values replace with correct :GUID eg:{BBD7D6C7-4475-44B7...}" + Environment.NewLine);
                File.AppendAllText(coname, @"MACPOOL1=Start#00:11:22:33:44:55|End#00:11:22:33:44:99|Next#00:11:22:33:44:55" + Environment.NewLine);
                File.AppendAllText(coname, @"MACPOOL1=Start#00:11:22:33:44:55|End#00:11:22:33:44:99|Next#00:11:22:33:44:55" + Environment.NewLine);
                shell = true;
            }

            if (!shell)
            {
                foreach (String FileLine in File.ReadAllLines(coname))
                {
                    if (FileLine.Contains("ADAPTER1"))
                    {
                        Adapter1 = FileLine.Substring(FileLine.IndexOf("#") + 1);
                    }
                    if (FileLine.Contains("ADAPTER2"))
                    {
                        Adapter2 = FileLine.Substring(FileLine.IndexOf("#") + 1);
                    }
                    if (FileLine.Contains("MACPOOL"))
                    {
                        String line = FileLine.Substring(FileLine.IndexOf("=") + 1);
                        MacPoolList.Add(new MacAddPool(line));
                    }
                }
            }

            return !shell;
        }

        public void CommitDone(int index)
        {
            String name = PathName + @"\rundone.txt";
            CommitPersist(name, index);
        }

        public void SaveInFlight()
        {
            lock (MyLock)
            {                
                //move the old 
                //save the inflight.txt
                //delete the old

                String namea = PathName + @"\inflight.txt.old";
                String nameb = PathName + @"\inflight.txt";
                if (File.Exists(namea))
                {
                    File.Delete(namea);
                }
                if (File.Exists(nameb))
                {
                    File.Move(nameb, namea);
                }
                SaveToFile(nameb);
                File.Delete(namea);
            }
        }

        private bool RunPscp(String PArgs)
        {
            String ProcName = PathName + @"\pscp.exe";
            String ProcArgs = @" -pw Designed&AssembledInCalifornia2229 " +
                               @" -hostkey  be:78:c7:80:b2:e2:30:4d:79:0b:2f:a3:72:2c:45:bf -scp " + PArgs;
            Process uproc = new Process();
            uproc.StartInfo.FileName = ProcName;
            uproc.StartInfo.Arguments = ProcArgs;
            uproc.StartInfo.CreateNoWindow = true;
            uproc.StartInfo.UseShellExecute = false;
            uproc.Start();
            uproc.WaitForExit();
            if (uproc.ExitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void UploadLog()
        {
            // get the finish time and  append to done.txt finally append the xomanuf.conf to it also
            // mv done.txt to done_TodaysDate.txt
            // zip up inflight.txt and done_TodaysDate.txt and xomanuf.conf 
            // and copy it up to x.o.ware using psftp (putty commandline tool)
            // delete the inflight.txt or inflight.txt.old
            // delete the files older than a month
            // At the end of the dailyrun, MacPoolList needs to be saved in the *Configuration file* to be loaded
            // the next shift.
            String namedone = PathName + @"\rundone.txt";
            String namedated = PathName + @"\rundone_" + TodaysDate + @".txt";
            String ZipName = PathName + @"\rundone_" + TodaysDate + @".zip";
            String nameb = PathName + @"\inflight.txt";
            String coname = PathName + @"\xomanuf.conf";
	    bool upSuccess;

            if (File.Exists(nameb))
            {
                if (File.Exists(namedone))
                {
                    SaveConFile();
                    String[] templinearray = File.ReadAllLines(coname);

                    File.AppendAllText(namedone, EndTime + Environment.NewLine);
                    File.AppendAllLines(namedone, templinearray);
                    File.Move(namedone, namedated);

                    using (ZipArchive zarch = ZipFile.Open(ZipName, ZipArchiveMode.Create))
                    {
                        zarch.CreateEntryFromFile(namedated, "rundone_" + TodaysDate + ".txt", CompressionLevel.Fastest);
                        zarch.CreateEntryFromFile(nameb, "inflight.txt", CompressionLevel.Fastest);
                    }

                    File.Delete(nameb);
                    File.Delete(nameb + ".old");
                    File.Delete(namedated);

                    String ProcArgs = ZipName + @" xomanuf@ns2.vpex.org:/incoming/" + StationName +
                                         @"_rundone_" + TodaysDate + @".zip ";
		            upSuccess = RunPscp(ProcArgs);
                    if (upSuccess)
                    {
                        File.Delete(ZipName + "sent");
                        File.Move(ZipName, ZipName + "sent");
                    }

                    string[] files = Directory.GetFiles(PathName);
                    foreach (string file in files)
                    {
                        if (file.Contains("rundone"))
                        {
                            FileInfo fi = new FileInfo(file);
                            if (fi.CreationTime < DateTime.Now.AddMonths(-1))
                                fi.Delete();
                        }
                    }
                }
            }
            else
            {
                // Last runs delivery didnt happen. 
                // or this is an error!!! No done log at the end of an entire run!
                string[] files = Directory.GetFiles(PathName);
                foreach (string file in files)
                {
                    if (file.Contains("rundone") && !file.Contains("sent"))
                    {
                        String ProcArgs = file + @" xomanuf@ns2.vpex.org:/incoming/" + StationName +
                                         @"_" + Path.GetFileName(file); 
			            upSuccess = RunPscp(ProcArgs);

                        if (upSuccess)
                        {
                            File.Delete(file + "sent");
                            File.Move(file, file + "sent");
                        }
                    }
                }

            }
        }

        public bool bootup()
        {
            //the finishtime or lastline contains "macpool" is a non-crash marker of last session
            //so at bootup time to do recovery the app will 
            // 1) look for inflight.txt or inflight.txt.old
            // 2) check the last line of done.txt to see marker
            //also if this was a crash send the log files to xoware immediately.

            //try loading the  existing in-flight data if we are recovering from a crash. (functions available for this)

            // next check for xomanuf.conf
            // if it does not exist or has only markervalues 
            // let user know to modify and restart
 
            bool reply = false;
            String coname = PathName + @"\xomanuf.conf";
            String nameb = PathName + @"\inflight.txt";
            String namedone = PathName + @"\rundone.txt";
            String namedated = PathName + @"\rundone_" + TodaysDate + @".txt";
            String ZipName = PathName + @"\rundone_" + TodaysDate + @".zip";

            if (File.Exists(coname))
            {
                foreach (String FileLine in File.ReadAllLines(coname))
                {
                    if (FileLine.Contains("marker values replace with correct"))
                    {
                        return reply;
                    }
                }
            }
            else
            {
                LoadConFile();
                return reply;
            }

            if (File.Exists(nameb) || File.Exists(nameb + ".old"))
            {
                if (File.Exists(namedone))
                {
                    String[] templines = File.ReadAllLines(namedone);
                    if (templines[templines.Count() - 1].Contains("MACPOOL") ||
                        templines[templines.Count() - 2].Contains("MACPOOL") ||
                        templines[templines.Count() - 3].Contains("MACPOOL") 
                        )
                    {
                        File.Delete(nameb);
                        File.Delete(nameb + ".old");
                        int index = 0;
                        while (File.Exists(namedone + index.ToString()))
                        {
                            index++;
                        }
                        File.Delete(namedone + index.ToString());
                        File.Move(namedone, namedone + index.ToString());
                        UploadLog();
                        //set reply true
                        //we are going to start new session.
                    }
                    else
                    {
                        reply = true;
                        //this is a genuine crash. load this nameb or nameb.old
                        if (!File.Exists(nameb))
                        {
                            File.Copy(nameb + ".old", nameb, true);
                        }
                        File.Delete(nameb + ".old");
                        //load the nameb file into exonetstacck
                        //that will automatically load the macpoollist
            		    reply = LoadConFile();
                        LoadFromFile(nameb);
                        return reply;
                    }
                }
                else if (File.Exists(namedated))   //sufficient resolution in date, this is no longer possible
                {
                    String[] templines = File.ReadAllLines(namedated);
                    if (templines[templines.Count() - 1].Contains("MACPOOL") ||
                        templines[templines.Count() - 2].Contains("MACPOOL") ||
                        templines[templines.Count() - 3].Contains("MACPOOL")
                        )
                    {
                        File.Delete(nameb);
                        File.Delete(nameb + ".old");
                        int index = 0;
                        while (File.Exists(namedated + index.ToString()))
                        {
                            index++;
                        }
                        File.Delete(namedated + index.ToString());
                        File.Move(namedated, namedated + index.ToString());
                        UploadLog();
                        //set reply true
                        //we are going to start new session.
                    }
                    else
                    {
                        //this cant be. dated file is one with endmarker
                    }
                }
                else
                {
                    //TODO: see if this is err condition. A very early crash?
                    //maybe just load it?        Actually for now do nothing!
                }
            }
            reply = true;
            // if no issues create a new rundone.txt
            // init it by copying the xomanuf.conf to it
            // write currenttime to it.
            // Load macpoollist
            File.Copy(coname, namedone, true);
            File.AppendAllText(namedone, StartTime + Environment.NewLine);
            reply = LoadConFile();
            return reply;
        }
    }



//TODO: WRAP ALL FIELDS IN PROPERTY ACCESSORS AND 
//	TAKE LOCKS INSIDE THE ACCESSORS TO MAKE 
//	EVERYTHING THREAD SAFE.
//	this means create backing store and a lock
//	for each one of the fields and a property
// c# .net classes: string, ping, physicaladdress, file, udpclient, printdoc, graphics_draw


    //exonet under test
    class ExoNetUT
    {
        private bool _Alive;
        private String _DynamicMac;
        private String _EtherMac1;
        private String _EtherMac2;
        private String _DynamicIP;
        private String _LinkLocalIP;
        private String _SerialNo;
        private String _BarcodeData;
        private Int16 _BootPhase; 
        private bool _PingTestStatus;
        private bool _BlinkingStatus;
        private bool _LabeledStatus;
        private bool _SvcdStatus;
        private bool _ReadyPending;
        private bool _ShinePending;


        private Object l_Alive;
        private Object l_DynamicMac;
        private Object l_EtherMac1;
        private Object l_EtherMac2;
        private Object l_DynamicIP;
        private Object l_LinkLocalIP;
        private Object l_SerialNo;
        private Object l_BarcodeData;
        private Object l_BootPhase; 
        private Object l_PingTestStatus;
        private Object l_BlinkingStatus;
        private Object l_LabeledStatus;
        private Object l_SvcdStatus;
        private Object l_ReadyPending;
        private Object l_ShinePending;


        public bool Alive
        {
            get
            {
                lock (l_Alive)
                {
                    return _Alive;
                }
            }
            set
            {
                lock (l_Alive)
                {
                    _Alive = value;
                }
            }
        }

        public String DynamicMac
        {
            get
            {
                lock (l_DynamicMac)
                {
                    return _DynamicMac;
                }
            }
            set
            {
                lock (l_DynamicMac)
                {
                    _DynamicMac = value;
                }
            }
        }

        public String EtherMac1
        {
            get
            {
                lock (l_EtherMac1)
                {
                    return _EtherMac1;
                }
            }
            set
            {
                lock (l_EtherMac1)
                {
                    _EtherMac1 = value;
                }
            }
        }

        public String EtherMac2
        {
            get
            {
                lock (l_EtherMac2)
                {
                    return _EtherMac2;
                }
            }
            set
            {
                lock (l_EtherMac2)
                {
                    _EtherMac2 = value;
                }
            }
        }

        public String DynamicIP
        {
            get
            {
                lock (l_DynamicIP)
                {
                    return _DynamicIP;
                }
            }
            set
            {
                lock (l_DynamicIP)
                {
                    _DynamicIP = value;
                }
            }
        }
        
        public String LinkLocalIP
        {
            get
            {
                lock (l_LinkLocalIP)
                {
                    return _LinkLocalIP;
                }
            }
            set
            {
                lock (l_LinkLocalIP)
                {
                    _LinkLocalIP = value;
                }
            }
        }
        
        public String SerialNo
        {
            get
            {
                lock (l_SerialNo)
                {
                    return _SerialNo;
                }
            }
            set
            {
                lock (l_SerialNo)
                {
                    _SerialNo = value;
                }
            }
        }
        
        public String BarcodeData
        {
            get
            {
                lock (l_BarcodeData)
                {
                    return _BarcodeData;
                }
            }
            set
            {
                lock (l_BarcodeData)
                {
                    _BarcodeData = value;
                }
            }
        }
        
        public Int16 BootPhase // only -1,0,1,2 allowed
        {
            get
            {
                lock (l_BootPhase)
                {
                    return _BootPhase;
                }
            }
            set
            {
                lock (l_BootPhase)
                {
                    _BootPhase = value;
                }
            }
        }
        
        public bool PingTestStatus
        {
            get
            {
                lock (l_PingTestStatus)
                {
                    return _PingTestStatus;
                }
            }
            set
            {
                lock (l_PingTestStatus)
                {
                    _PingTestStatus = value;
                }
            }
        }

        public bool BlinkingStatus
        {
            get
            {
                lock (l_BlinkingStatus)
                {
                    return _BlinkingStatus;
                }
            }
            set
            {
                lock (l_BlinkingStatus)
                {
                    _BlinkingStatus = value;
                }
            }
        }
        
        public bool LabeledStatus
        {
            get
            {
                lock (l_LabeledStatus)
                {
                    return _LabeledStatus;
                }
            }
            set
            {
                lock (l_LabeledStatus)
                {
                    _LabeledStatus = value;
                }
            }
        }

        public bool SvcdStatus
        {
            get
            {
                lock (l_SvcdStatus)
                {
                    return _SvcdStatus;
                }
            }
            set
            {
                lock (l_SvcdStatus)
                {
                    _SvcdStatus = value;
                }
            }
        }
        
        public bool ReadyPending
        {
            get
            {
                lock (l_ReadyPending)
                {
                    return _ReadyPending;
                }
            }
            set
            {
                lock (l_ReadyPending)
                {
                    _ReadyPending = value;
                }
            }
        }

        public bool ShinePending
        {
            get
            {
                lock (l_ShinePending)
                {
                    return _ShinePending;
                }
            }
            set
            {
                lock (l_ShinePending)
                {
                    _ShinePending = value;
                }
            }
        }


        public ExoNetUT()
        {
            _Alive = false;
            _DynamicMac = "";
            _EtherMac1 = "";
            _EtherMac2 = "";
            _DynamicIP = "";
            _LinkLocalIP = "";
            _SerialNo = "";
            _BarcodeData = "";
            _PingTestStatus = false;
            _BlinkingStatus = false;
            _LabeledStatus = false;
            _SvcdStatus = false;
            _ReadyPending = false;
            _ShinePending = false;

            l_Alive = new Object();
            l_DynamicMac = new Object();
            l_EtherMac1 = new Object();
            l_EtherMac2 = new Object();
            l_DynamicIP = new Object();
            l_LinkLocalIP = new Object();
            l_SerialNo = new Object();
            l_BarcodeData = new Object();
            l_BootPhase = new Object();
            l_PingTestStatus = new Object();
            l_BlinkingStatus = new Object();
            l_LabeledStatus = new Object();
            l_SvcdStatus = new Object();
            l_ReadyPending = new Object();
            l_ShinePending = new Object();

            EtherMac1 = "ff:ff:ff:ff:ff:ff";
            EtherMac2 = "ff:ff:ff:ff:ff:ff";
            BarcodeData = "";
        }

        public ExoNetUT(String FromString)
        {
            //Format: '|' delimited Name#value
            // convert a single line string to an entire single instance 
            FromString = FromString.Trim();
            String[] DelimArray = { "Alive#", 
                                    "DynamicMac#", 
                                    "EtherMac1#", 
                                    "EtherMac2#",
                                    "DynamicIP#",
                                    "LinkLocalIP#",
                                    "SerialNo#",
                                    "BarcodeData#",
                                    "BootPhase#",
                                    "PingTestStatus#",
                                    "BlinkingStatus#",
                                    "LabeledStatus#",
                                    "SvcdStatus#",
                                    "ReadyPending#",
                                    "ShinePending#"
                                  };
            var StringPieces = FromString.Split(DelimArray, 13, StringSplitOptions.RemoveEmptyEntries);
            Alive = Boolean.Parse(StringPieces[0].Trim('|'));
            DynamicMac = StringPieces[1].Trim('|');
            EtherMac1 = StringPieces[2].Trim('|');
            EtherMac2 = StringPieces[3].Trim('|');
            DynamicIP = StringPieces[4].Trim('|');
            LinkLocalIP = StringPieces[5].Trim('|');
            SerialNo = StringPieces[6].Trim('|');
            BarcodeData = StringPieces[7].Trim('|');
            BootPhase = Int16.Parse(StringPieces[8].Trim('|'));
            PingTestStatus = Boolean.Parse(StringPieces[9].Trim('|'));
            BlinkingStatus = Boolean.Parse(StringPieces[10].Trim('|'));
            LabeledStatus = Boolean.Parse(StringPieces[11].Trim('|'));
            SvcdStatus = Boolean.Parse(StringPieces[12].Trim('|'));
            ReadyPending = Boolean.Parse(StringPieces[13].Trim('|'));
            ShinePending = Boolean.Parse(StringPieces[14].Trim('|'));

        }

        public override String ToString()
        {
            // convert an entire instance into a single line of string
            String InstString =     "Alive#" + Alive.ToString() + "|" +
                                    "DynamicMac#" + DynamicMac + "|" +
                                    "EtherMac1#" + EtherMac1 + "|" +
                                    "EtherMac2#" + EtherMac2 + "|" +
                                    "DynamicIP#" + DynamicIP + "|" +
                                    "LinkLocalIP#" + LinkLocalIP + "|" +
                                    "SerialNo#" + SerialNo + "|" +
                                    "BarcodeData#" + BarcodeData + "|" +
                                    "BootPhase#" + BootPhase.ToString() + "|" +
                                    "PingTestStatus#" + PingTestStatus.ToString() + "|" +
                                    "BlinkingStatus#" + BlinkingStatus.ToString() + "|" +
                                    "LabeledStatus#" + LabeledStatus.ToString() + "|" +
                                    "SvcdStatus#" + SvcdStatus.ToString() + "|" +
                                    "ReadyPending#" + ReadyPending.ToString() + "|" +
                                    "ShinePending#" + ShinePending.ToString();
                
            return InstString;
        }
    }

    static class PhyMacAddr 
    {
        public static void increment(MacAddPool lv)
        {
            lv.NextAvailable =  new MacAddress((UInt48)(lv.NextAvailable.ToValue() + 1));
        }
    }

    class MacAddPool
    {

        public MacAddress StartInclusive;
        public MacAddress EndInclusive;
        public MacAddress NextAvailable;

        public MacAddPool(MacAddress _start, MacAddress _end)
        {
            StartInclusive = _start;
            EndInclusive = _end;
            NextAvailable = StartInclusive;
        }
        public MacAddPool(MacAddress _start, MacAddress _end, MacAddress _nxt)
        {
            StartInclusive = _start;
            EndInclusive = _end;
            NextAvailable = _nxt;
        }

        public bool IsExhausted()
        {
            if (NextAvailable.ToValue() <= EndInclusive.ToValue())
            {
                return false;
            }
            return true;
        }
        // add tostring and fromstring.
        public MacAddPool(String FileLine)
        {
            //format: 'Start#00:11:22:33:44:55|End#00:11:22:33:44:55|Next#00:11:22:33:44:55'
            FileLine = FileLine.Trim();
            String[] DelimArray = {"Start#","End#","Next#"};
            var StringPieces = FileLine.Split(DelimArray, 3,  StringSplitOptions.RemoveEmptyEntries);
            String StartString = StringPieces[0].Trim('|');
            String EndString = StringPieces[1].Trim('|');
            String NextString = StringPieces[2].Trim('|');
            StartInclusive = new MacAddress(StartString);
            EndInclusive = new MacAddress(EndString);
            NextAvailable = new MacAddress(NextString);
        }
        public override String ToString()
        {
            String FileLine = "Start#" + StartInclusive.ToString() + "|"
                                + "End#" + EndInclusive.ToString() + "|"
                                + "Next#" + NextAvailable.ToString();
            return FileLine;
        }
    }

    class StatusBar
    {
        public Int32 DayRunCount;
        private Int16 _DaySerial;
        private Object l_DaySerial;
        public String BadRunCount;
        //"X:Y" Unclaimed,Snooped:Served
        public Int16 CurrentlyConnected;
        public Int16 CurrentlyReady;

        public void UpdateStatus(Object _enstack)
        {
            //TODO: find out if this is going to create duplicates, the cast??
            var EntireStack = _enstack as List<ExoNetUT>;
            CurrentlyConnected = 0;
            CurrentlyReady = 0;
            foreach (ExoNetUT IterUT in EntireStack)
            {
                if (IterUT.Alive == true)
                {
                    CurrentlyConnected++;
                    if (IterUT.PingTestStatus == true && IterUT.ReadyPending == true)
                    {
                        CurrentlyReady++;
                    }
                }
            }
        }

        public StatusBar()
        {
            DayRunCount = 0;
            CurrentlyConnected = 0;
            CurrentlyReady = 0;
            BadRunCount = "X:Y";
            _DaySerial = 0;
            l_DaySerial = new Object();
        }

        public Int16 DaySerial
        {
            get
            {
                lock (l_DaySerial)
                {
                    return _DaySerial;
                }
            }
            set
            {
                lock (l_DaySerial)
                {
                    _DaySerial = value;
                }
            }
        }

        // add tostring and fromstring
        public StatusBar(String FileLine)
        {
            //format: 'Start#00:11:22:33:44:55|End#00:11:22:33:44:55|Next#00:11:22:33:44:55'
            FileLine = FileLine.Trim();
            String[] DelimArray = {"DayRunCount#","DaySerial#","CurrentlyConnected#","CurrentlyReady#"};
            var StringPieces = FileLine.Split(DelimArray, 4,  StringSplitOptions.RemoveEmptyEntries);
            DayRunCount = Int32.Parse(StringPieces[0].Trim('|'));
            _DaySerial = Int16.Parse(StringPieces[1].Trim('|'));
            CurrentlyConnected = Int16.Parse(StringPieces[1].Trim('|'));
            CurrentlyReady = Int16.Parse(StringPieces[1].Trim('|'));

        }

        public override String ToString()
        {
            String FileLine = "DayRunCount#" + DayRunCount.ToString() + "|"
                                + "DaySerial#" + DaySerial.ToString() + "|"
                                + "CurrentlyConnected#" + CurrentlyConnected.ToString() + "|"
                                + "CurrentlyReady#" + CurrentlyReady.ToString();
            return FileLine;
        }

    }


//    public delegate void CallBackFType(Packet packet);
    class libpcapObj
    {
        private int Adapter;
        public static IList<LivePacketDevice> allDevices;
        HandlePacket PacketHandler;
        String MyBPF;

        public libpcapObj(String _adapter, HandlePacket _PacketHandler, String _MyBPF)
        {
            PacketHandler = _PacketHandler;
            MyBPF = _MyBPF;
            allDevices = LivePacketDevice.AllLocalMachine;

            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                if (device.Name.Contains(_adapter))
                {
                    Adapter = i;
                }
            }
        }

        public void StartCapture()
        {

            // Take the selected adapter
            PacketDevice selectedDevice = allDevices[Adapter];

            // Open the device
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {
                // Check the link layer. We support only Ethernet for simplicity.
                if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                {
                    return;
                }

                // Compile the filter
                using (BerkeleyPacketFilter filter = communicator.CreateFilter(MyBPF))
                {
                    // Set the filter
                    communicator.SetFilter(filter);
                }

                // start the capture
                communicator.ReceivePackets(0, PacketHandler);
            }

        }
//MyBPF = "ip and udp";
/*        private static void PacketHandler(Packet packet)
        {
            _capturelist.Add(new SomeDataStructure(packet.Timestamp.ToString("MM-dd hh:mm:ss.fff") + " length:" + packet.Length));
            IpV4Datagram ip = packet.Ethernet.IpV4;
            UdpDatagram udp = ip.Udp;

            _capturelist.Add(new SomeDataStructure(ip.Source + ":" + udp.SourcePort + " -> " + ip.Destination + ":" + udp.DestinationPort));
            Console.WriteLine(_capturelist[_capturelist.Count - 1].MyData);

        }*/
    }

    class OpentftpdObj
    {
        public OpentftpdObj()
        {
            //TODO
            //init object to control the opentftpd service
        }
    }

    public delegate String DelegateType( String _RcvdData, String _RemoteClient);
    class SignalProtocol
    {
        private UdpClient UdpServer;
        private IPEndPoint RemoteIP;

        public SignalProtocol()
        {
            UdpServer = new UdpClient(36969);
            RemoteIP = new IPEndPoint(IPAddress.Any, 0);
            
        }

        //This is a listening loop sitting in an infinite iteration
        // to udp listen to one of 2 things: either SvcReq or Ready
        // as either is received, update the ExoNetUT list's corresponding
        // object for this EN and send back the apropriate response.
        public void DoWork(DelegateType SPLogic)
        {
            Byte[] data;
            String RcvdData;
            String ResponseData;
            bool responsibleloop;
            String RemoteClient;

            // I can receive "SvcReq..." or "Ready" or "Blinking" 
            // Response "ManufData..." or "ShineFlasher". No response to send for blinking just internal action.
            while (true)
            {
                responsibleloop = false;
                data = UdpServer.Receive(ref RemoteIP);
                RcvdData = Encoding.ASCII.GetString(data);
                RemoteClient = RemoteIP.Address.ToString();
                ResponseData = SPLogic(RcvdData, RemoteClient);
                if (!ResponseData.Contains("NoResponse"))
                {
                    responsibleloop = true;
                }
                if (responsibleloop)
                {
                    data = Encoding.ASCII.GetBytes(ResponseData);
                    UdpServer.Send(data, data.Length, RemoteIP);
                }
            	RemoteIP = new IPEndPoint(IPAddress.Any, 0);
            }
        }
    }

    public delegate void HookDelegate(String ScanValue);
    class BarCodeScanner
    {

        public BarCodeScanner()
        {
            SScanData = new StringBuilder();
            SScanKeyConverter = new KeyConverter();
        }


        private bool SLeftCtrlDown = false;
        private bool SScanShiftDown = false;
        private bool SScanning = false;
        private StringBuilder SScanData;
        private KeyConverter SScanKeyConverter;
        public event PropertyChangedEventHandler ScanActionEvent;
        private HookDelegate PostScanCallBack;


        public void ResetEventHandlerChain()
        {
            this.ScanActionEvent = null;
        }

        public void FireEnableEvent(HookDelegate _hookfunc)
        {
            if (ScanActionEvent != null)
            {
                PostScanCallBack = _hookfunc;
                ScanActionEvent("EnableScan", new PropertyChangedEventArgs("nothing here"));
            }
        }

        public void Scan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
            {
                SLeftCtrlDown = false;
            }
            else if (SScanning)
            {
                // Handle all Keyups while scanning to preven other events from catching them
                e.Handled = true;
                if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                {
                    // Note - We dont track shift keys separately. It is possible for us to get
                    // wrong data if the user were to press 2 shift keys and then let up only 1 but we only track
                    // this when scanning and a bar code scanner should be consistent.
                    SScanShiftDown = false;
                }
            }
            //TESTING//listBox1.Items.Add("Up: " + e.Key.ToString());
        }

        public void Scan_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Note: This looks for ALL Ctrl-B combos, including Ctrl-Alt-B, Ctrl-Shift-B, etc.
            //
            // Note also that we only look for LeftCtrl, other scanners
            // could possibly use RightCtrl but I think not. The scanner is actaully sending
            // 0x02, but WPF is likely the one interpreting it as Ctrl-B according to old keyboard semantics.
            if (e.Key == Key.LeftCtrl)
            {
                SLeftCtrlDown = true;
                e.Handled = true;
            }
            else
            {
                if (SScanning)
                {
                    // Handle all Keydowns while scanning to preven other events from catching them
                    e.Handled = true;
                    if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                    {
                        SScanShiftDown = true;
                    }
                    else if (SLeftCtrlDown && e.Key == Key.C)
                    {
                        SScanning = false;

                        if (ScanActionEvent != null)
                        {
                            ScanActionEvent("StopScan", new PropertyChangedEventArgs(SScanData.ToString()));
                            PostScanCallBack(SScanData.ToString());
                        }
                    }
                    else
                    {
                        string xChar = SScanKeyConverter.ConvertToString(e.Key);
                        if (!SScanShiftDown)
                        {
                            xChar = xChar.ToLower();
                        }
                        SScanData.Append(xChar);
                    }
                }
                else
                {
                    if (SLeftCtrlDown && e.Key == Key.B)
                    {
                        SScanning = true;
                        SScanData.Clear();
                        SScanShiftDown = false;
                        e.Handled = true;
                    }
                }
            }
        }
    }


    class LabelPrinter
    {
        public static void PrintLabel(String mac_addr, String sr_no)
        {
            PrintDialog printDlg = new PrintDialog();

            /* TODO: _complete_for_more_precision
            var printer = new PrintServer().GetPrintQueues().ToList().FirstOrDefault(x => x.FullName.Contains("XPS"));
            if (printer == null)
            {
                MessageBox.Show("No XPS printer found");
                return;
            }
            printDialog.PrintQueue = printer;
            printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
            printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaLetter);
            printDialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
            */

            DrawingVisual visual = new DrawingVisual();
            using (LabelContext = visual.RenderOpen())
            {

                TypeSetLine("x.o.ware, inc.   ExoNet", "Tahoma", 14, 30, 10);
                TypeSetLine("TM", "Tahoma", 6, 170, 10);
                TypeSetLine("101-5-1.0", "Tahoma", 6, 90, 25);
                TypeSetLine("Designed & Assembled in California", "Tahoma", 11, 20, 40);
                TypeSetLine("Default Username: admin", "Tahoma", 8, 20, 60);
                TypeSetLine("Default Password: 123456", "Tahoma", 8, 20, 70);
                TypeSetLine("Default IP Address on eth2: 192.168.2.1", "Tahoma", 8, 20, 80);
                TypeSetLine("Default URL on eth1: http://exonet.local", "Tahoma", 8, 20, 90);
                //typeset barcode
                TypeSetLine("MAC ADR: " + mac_addr, "Consolas", 9, 20, 140);
                //typeset barcode
                TypeSetLine("S/N: " + sr_no, "Consolas", 8, 20, 180);

                String TrimAddr = mac_addr.Replace(":", "");
                var MacBarRect = new Rect(20, 105, 160, 30);
                BarcodeDraw.Draw(LabelContext, TrimAddr, new BarcodeMetrics1d(1, 2, 30), MacBarRect);

                var SrNoRect = new Rect(20, 155, 160, 25);
                BarcodeDraw.Draw(LabelContext, sr_no, new BarcodeMetrics1d(1, 2, 30), SrNoRect);


                String FCCXAML = @"<Canvas xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Name=""svg3336"" Width=""500"" Height=""420"">
                <Canvas.Resources/>
                <Path xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Name=""path3338"" StrokeThickness=""40"" Stroke=""#FF000000"" Data=""M403.762 263.297 A92.5 92.5 0 1 1 402.733 156.056 M473.261 311.743 A177.218 177.218 0 1 1 471.288 106.283""/>
                <Path xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Name=""path3340"" Fill=""#000000"" Data=""M134 190H51.78V57.47H149.29L179.29 17.47H11.78V400L51.78 373.3 V230H134Z""/>
                </Canvas>
                ";
                Canvas FCCLogo = XamlReader.Parse(FCCXAML) as Canvas;
                VisualBrush FCCIcon = new VisualBrush(FCCLogo);
                LabelContext.DrawGeometry(FCCIcon, new Pen(Brushes.Transparent, 0), new RectangleGeometry(new Rect(120, 60, 25, 20)));

                String CEXAML = @"<Canvas xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Name=""svg2"" Width=""279.8497"" Height=""200"">
                <Canvas.Resources/>
                <Canvas Name=""layer1"">
                <Canvas.RenderTransform>
                <TranslateTransform X=""-1.7385019"" Y=""-487.18166""/>
                </Canvas.RenderTransform>
                <Canvas Name=""g3004"">
                <Canvas.RenderTransform>
                <TranslateTransform X=""2.2385229"" Y=""-365.68055""/>
                </Canvas.RenderTransform>
                <Path xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Name=""path3006"" Fill=""#FF000000"" StrokeThickness=""0.37558964"" Stroke=""#FF000000"" Data=""m 109.48115 853.55031 0 30.15996 a 69.868543 69.868543 0 1 0 0 138.30383 l 0 30.16 a 99.812205 99.812205 0 1 1 0 -198.62379 z""/>
                <Path xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Name=""path3008"" Fill=""#FF000000"" StrokeThickness=""0.37558964"" Stroke=""#FF000000"" Data=""m 200.93511 937.89037 a 69.868543 69.868543 0 0 1 78.22678 -54.1801 l 0 -30.15996 a 99.812205 99.812205 0 1 0 0 198.62379 l 0 -30.16 a 69.868543 69.868543 0 0 1 -78.22678 -54.18008 l 58.26434 0 0 -29.94365 -58.26434 0 z""/>
                </Canvas>
                </Canvas>
                </Canvas>
                ";
                Canvas CELogo = XamlReader.Parse(CEXAML) as Canvas;
                VisualBrush CEIcon = new VisualBrush(CELogo);
                LabelContext.DrawGeometry(CEIcon, new Pen(Brushes.Transparent, 0), new RectangleGeometry(new Rect(150, 60, 25, 20)));

                String ROHSXAML = @"<Canvas xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Name=""svg3381"" Width=""146.862"" Height=""209.074"">
                <Canvas Name=""Layer_x0020_2"">
                <Canvas Name=""_97073168"">
                <Rectangle xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Canvas.Left=""11.7563"" Canvas.Top=""179.338"" Width=""121.86"" Height=""29.7355"" Name=""_98270272"" Fill=""#000000""/>
                <Path xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Name=""_97972288"" Fill=""#000000"" Data=""M65.5683 52.478l25.6733 0 0 8.07196 -25.6733 0 0 -8.07196zm-8.11524 76.862l0 5.90403 -7.63045 0 0 -5.94899c-0.835184 -0.179852 -1.47794 -0.884109 -1.55271 -1.76891l-8.17407 -96.6335 -5.71388 0 0 -3.84325 4.84046 0c0.792103 -3.88435 4.62882 -6.70237 8.40819 -8.32253l0 -2.79692 7.17508 0 0 0.755162c3.78056 -0.812505 7.58707 -1.389 11.4663 -1.56826l0 -1.26362 19.8582 0 0 1.73811c3.74035 0.13271 7.54488 0.679893 11.0112 1.92272 3.19713 1.14626 6.08388 2.87595 8.35154 5.39874 1.01989 -1.39425 2.66788 -2.30035 4.52721 -2.30035 3.09532 0 5.60553 2.51021 5.60553 5.60553 0 3.09532 -2.51021 5.60553 -5.60553 5.60553 -0.919069 0 -1.78684 -0.221646 -2.5524 -0.613835l-0.61027 6.80339 8.43454 0 0 14.1313 -9.70212 0 -5.32188 59.3296c5.4539 1.183 9.54099 6.03733 9.54099 11.8449 0 6.69316 -5.42785 12.121 -12.121 12.121 -4.50156 0 -8.43107 -2.45554 -10.5216 -6.09983l-29.7134 0zm40.2349 -10.1693c2.29054 0 4.14809 1.85755 4.14809 4.14809 0 2.29054 -1.85755 4.14809 -4.14809 4.14809 -2.29054 0 -4.14809 -1.85755 -4.14809 -4.14809 0 -2.29054 1.85755 -4.14809 4.14809 -4.14809zm-11.9255 6.32603c-0.128254 -0.706732 -0.1955 -1.43426 -0.1955 -2.17794 0 -6.27334 4.76817 -11.4352 10.8774 -12.058l5.30287 -59.1164 -0.418335 0 0 -14.1313 1.68592 0 0.638694 -7.12041 -59.7027 0 8.00224 94.6041 33.8095 0zm11.9255 -8.62429c3.55961 0 6.44636 2.88675 6.44636 6.44636 0 3.55961 -2.88675 6.44636 -6.44636 6.44636 -3.55961 0 -6.44636 -2.88675 -6.44636 -6.44636 0 -3.55961 2.88675 -6.44636 6.44636 -6.44636zm8.88744 -75.7149l-0.703464 7.84239 6.27562 0 0 -7.84239 -5.57215 0zm-51.7697 -20.5436l0 6.43546 49.0542 0c-1.9246 -2.89497 -4.76103 -4.76767 -8.00957 -5.93235 -2.99252 -1.07297 -6.3595 -1.55479 -9.72014 -1.68067l0 3.15375 -19.8582 0 0 -3.63844c-3.88019 0.196392 -7.68631 0.813891 -11.4663 1.66225zm-7.17508 6.43546l0 -4.08906c-1.92371 1.00553 -3.73778 2.3875 -4.43154 4.08906l4.43154 0zm21.7858 -10.0517l13.5693 0 0 2.44801 -13.5693 0 0 -2.44801z""/>
                <Polygon xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Points=""146.862,0 71.6744,76.2965 0,0.973441 0,6.5346 68.9823,79.0283 1.79704,147.204 4.57445,149.846 71.6258,81.8064 135.869,149.32 138.647,146.678 74.318,79.0746 146.862,5.46064 "" Name=""_98273200"" FillRule=""evenodd"" Fill=""#000000""/>
                </Canvas>
                </Canvas>
                </Canvas>
                ";
                Canvas ROHSLogo = XamlReader.Parse(ROHSXAML) as Canvas;
                VisualBrush ROHSIcon = new VisualBrush(ROHSLogo);
                LabelContext.DrawGeometry(ROHSIcon, new Pen(Brushes.Transparent, 0), new RectangleGeometry(new Rect(180, 60, 20, 30)));


            }
            printDlg.PrintVisual(visual, "LABEL");
        }

        private static readonly BarcodeDraw BarcodeDraw = BarcodeDrawFactory.Code128WithChecksum;
        private static DrawingContext LabelContext;
        private static void TypeSetLine(String Data, String TFace, int TFSize, int XOffset, int YOffset)
        {
            //TypeSet the lines
            FormattedText formattedText = new FormattedText(Data, CultureInfo.GetCultureInfo("en-us"),
                                                            FlowDirection.LeftToRight, new Typeface(TFace), TFSize, Brushes.Black);
            LabelContext.DrawText(formattedText, new Point(XOffset, YOffset));
        }

    }

    class ConsoleToken
    {
        public bool Status;
        public String Message;
        public ConsoleToken()
        {
            Status = false;
            Message = " ";
        }
    }

    class StatusLabelCombo
    {
        public String Status;
        public String LabelBox;
        public StatusLabelCombo()
        {
            Status = "";
            LabelBox = "";
        }
        public StatusLabelCombo(String FromString)
        {


            string[] stringSeparators = new string[] { "#]%[#" };
            string[] result;


            // Split a string delimited by another string and return all elements.
            result = FromString.Split(stringSeparators, StringSplitOptions.None);
            if (result.Length == 1)
            {
                Status = result[0];
                LabelBox = "";
            }
            else if (result.Length == 2)
            {
                Status = result[0];
                LabelBox = result[1];
            }
            else
            {
                Status = "";
                LabelBox = "";
            }
        }
        public override string ToString()
        {
            return Status + "#]%[#" + LabelBox;
        }
    }
}
