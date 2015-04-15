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
            File.Create(FileName);
            File.AppendAllText(FileName, "InFlightFileSection:     MacPoolList");
            foreach (MacAddPool IterMAC in MacPoolList)
            {
                File.AppendAllText(FileName, IterMAC.ToString());
            }

            File.AppendAllText(FileName, "InFlightFileSection:     TodayStatus");
            File.AppendAllText(FileName, TodayStatus.ToString());

            File.AppendAllText(FileName, "InFlightFileSection:     ExoNetStack");
            foreach (ExoNetUT IterUT in ExoNetStack)
            {
                File.AppendAllText(FileName, IterUT.ToString());
            }
             
        }

        private void LoadFromFile(String FileName)
        {
            // Read one line at a time and load to the stack_list by calling fromstring ctor
            // catch delimiter and
            // load macpoollist from next full line strings read from file. using fromstring ctor
            // catch delimiter and
            // load today status into memory from a line string.
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
            int index = 0;
            for (index = 0; index < ExoNetStack.Count; index++)
            {
                if (ExoNetStack[index].Alive == true)
                {
                    if (ExoNetStack[index].PingTestStatus == true && ExoNetStack[index].ReadyPending == true)
                    {
                        break;
                    }
                }
            }
            return index;
        }

        //Make a slot dead
        private void CommitPersist(string FileName, int index)
        {
            // Take the  EN at list[index] and convert tostring and save to 
            // Filename 
            // change its Alive to false;
            File.AppendAllText(FileName, ExoNetStack[index].ToString());
            lock (CriticalSection)
            {
                ExoNetStack[index].Alive = false;
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
            String coname = PathName + @"\xomanuf.conf";
            File.Delete(coname);
            File.AppendAllText("ADAPTER1#" + Adapter1, coname);
            File.AppendAllText("ADAPTER2#" + Adapter2, coname);
            for (int index=0; index < MacPoolList.Count; index++)
            {
                MacAddPool IterMAC = MacPoolList[index];
                File.AppendAllText(coname, "MACPOOL"+index.ToString()+"="+IterMAC.ToString());
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
            else
            {
                File.AppendAllText(coname, "ADAPTER1#marker values replace with correct :GUID eg:{BBD7D6C7-4475-44B7...}");
                File.AppendAllText(coname, "ADAPTER2#marker values replace with correct :GUID eg:{BBD7D6C7-4475-44B7...}");
                File.AppendAllText(coname, @"MACPOOL1=Start#00:11:22:33:44:55|End#00:11:22:33:44:99|Next#00:11:22:33:44:55");
                File.AppendAllText(coname, @"MACPOOL1=Start#00:11:22:33:44:55|End#00:11:22:33:44:99|Next#00:11:22:33:44:55");
            }
            return shell;
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
                File.Move(nameb, namea);
                SaveToFile(nameb);
                File.Delete(namea);
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

            ZipArchive zarch = ZipFile.Open(ZipName, ZipArchiveMode.Create);
            if (File.Exists(nameb))
            {
                if (File.Exists(namedone))
                {
                    SaveConFile();
                    String[] templinearray = File.ReadAllLines(coname);

                    File.AppendAllText(namedone, EndTime);
                    File.AppendAllLines(namedone, templinearray);
                    File.Move(namedone, namedated);

                    zarch.CreateEntryFromFile(namedated, "rundone_" + TodaysDate + ".txt", CompressionLevel.Fastest);
                    zarch.CreateEntryFromFile(nameb, "inflight.txt", CompressionLevel.Fastest);

                    File.Delete(nameb);
                    File.Delete(nameb + ".old");

                    String ProcArgs = PathName + @"\pscp.exe " +
                                        " -pw Designed&AssembledInCalifornia2229 " +
                                        " -hostkey ab:f4:6d:e4:d9:cd:c9:af:0a:1b:41:f1:25:59:9e:d8 " +
                                        ZipName + " xomanuf@ns2.vpex.org:/home/xomanuf/" + StationName +
                                        "_rundone_" + TodaysDate + ".zip ";
                    /*
                    var uproc = new ProcessStartInfo();
                    uproc.UseShellExecute = true;
                    uproc.WorkingDirectory = @"C:\Windows\System32";
                    uproc.FileName = @"C:\Windows\System32\cmd.exe";
                    uproc.Verb = "runas";
                    uproc.Arguments = "/c " + ProcArgs;
                    uproc.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(uproc);
                    */
                    Process uproc = null;
                    uproc = Process.Start(ProcArgs);
                    uproc.WaitForExit();
                    if (uproc.ExitCode == 0)
                    {
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
                        String ProcArgs = PathName + @"\pscp.exe " +
                                         " -pw Designed&AssembledInCalifornia2229 " +
                                         " -hostkey ab:f4:6d:e4:d9:cd:c9:af:0a:1b:41:f1:25:59:9e:d8 " +
                                         file + " xomanuf@ns2.vpex.org:/home/xomanuf/" + StationName +
                                         "_" + Path.GetFileName(file); 

                        Process uproc = null;
                        uproc = Process.Start(ProcArgs);
                        uproc.WaitForExit();
                        if (uproc.ExitCode == 0)
                        {
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
                        File.Move(namedone, namedone + index.ToString());
                        UploadLog();
                        //set reply true
                        //we are going to start new session.
                    }
                    else
                    {
                        reply = true;
                        //this is a genuine crash. load this nameb or nameb.old
                        if (File.Exists(nameb))
                        {
                            File.Delete(nameb + ".old");
                        }
                        else
                        {
                            File.Move(nameb + ".old", nameb);
                        }
                        //load the nameb file into exonetstacck
                        //that will automatically load the macpoollist
                        LoadFromFile(nameb);
                        return reply;
                    }
                }
                else if (File.Exists(namedated))
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
            File.Copy(coname, namedone);
            File.AppendAllText(namedone, StartTime);
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
            l_Alive = new Object();
            Alive = false;
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

        public string ToString()
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
        public static MacAddress increment(MacAddress lv)
        {
            return new MacAddress((UInt48)(lv.ToValue() + 1));
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
        public String ToString(MacAddPool Entry)
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
                }
                if (IterUT.PingTestStatus == true && IterUT.ReadyPending == true)
                {
                    CurrentlyReady++;
                }
            }
        }

        public StatusBar()
        {
            DayRunCount = 0;
            CurrentlyConnected = 0;
            CurrentlyReady = 0;
        }

        // add tostring and fromstring
        public StatusBar(String FileLine)
        {
            //format: 'Start#00:11:22:33:44:55|End#00:11:22:33:44:55|Next#00:11:22:33:44:55'
            FileLine = FileLine.Trim();
            String[] DelimArray = {"DayRunCount#","BadRunCount#","CurrentlyConnected#","CurrentlyReady#"};
            var StringPieces = FileLine.Split(DelimArray, 4,  StringSplitOptions.RemoveEmptyEntries);
            DayRunCount = Int32.Parse(StringPieces[0].Trim('|'));
            BadRunCount = StringPieces[1].Trim('|');
            CurrentlyConnected = Int16.Parse(StringPieces[1].Trim('|'));
            CurrentlyReady = Int16.Parse(StringPieces[1].Trim('|'));

        }

        public String ToString(StatusBar Entry)
        {
            String FileLine = "DayRunCount#" + DayRunCount.ToString() + "|"
                                + "BadRunCount#" + BadRunCount.ToString() + "|"
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
                TypeSetLine("Default Username: admin", "Tahoma", 8, 60, 60);
                TypeSetLine("Default Password: 123456", "Tahoma", 8, 60, 70);
                TypeSetLine("Default IP Address on eth2: 192.168.2.1", "Tahoma", 8, 35, 80);
                TypeSetLine("Default URL on eth1: http://exonet.local", "Tahoma", 8, 35, 90);
                //typeset barcode
                TypeSetLine("MAC ADR: " + mac_addr, "Consolas", 9, 20, 140);
                //typeset barcode
                TypeSetLine("S/N: " + sr_no, "Consolas", 8, 20, 180);

                var MacBarRect = new Rect(20, 105, 160, 30);
                BarcodeDraw.Draw(LabelContext, mac_addr, new BarcodeMetrics1d(1, 2, 30), MacBarRect);

                var SrNoRect = new Rect(20, 155, 160, 25);
                BarcodeDraw.Draw(LabelContext, sr_no, new BarcodeMetrics1d(1, 2, 30), SrNoRect);

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
 
}
