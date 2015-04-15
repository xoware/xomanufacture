﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using NETCONLib;


namespace xomanufacture
{
    public class IcsManager
    {
        private static readonly INetSharingManager SharingManager = new NetSharingManager();

        public static IEnumerable<NetworkInterface> GetIPv4EthernetAndWirelessInterfaces()
        {
            return
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.Supports(NetworkInterfaceComponent.IPv4)
                where (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                   || (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                   || (nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                select nic;
        }

        public static void DisableAllShares()
        {
            INetSharingEveryConnectionCollection connections = SharingManager.EnumEveryConnection;
            foreach (INetConnection con in connections)
            {
                try
                {

                    INetSharingConfiguration config = GetConfiguration(con);

                    if (config.SharingEnabled)
                    {
                        Console.WriteLine("Sharing was enabled.  Disabeling: " + con.ToString());

                        config.DisableSharing();
                    }
                }
                catch (System.Runtime.InteropServices.ExternalException ex)
                {
                    Console.WriteLine("DisableAllShares InteropServices.ExternalException: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DisableAllShares EX: " + ex.Message);
                }
            }
        }

        public static NetShare GetCurrentlySharedConnections()
        {
            INetConnection sharedConnection = null;
            INetConnection homeConnection = null;
            INetSharingEveryConnectionCollection connections = SharingManager.EnumEveryConnection;
            foreach (INetConnection c in connections)
            {
                try
                {

                    INetSharingConfiguration config = GetConfiguration(c);
                    if (config.SharingEnabled)
                    {
                        if (config.SharingConnectionType == tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC)
                        {
                            sharedConnection = c;
                            Console.WriteLine("SharedConnection=" + c.ToString());
                        }
                        else if (config.SharingConnectionType == tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE)
                        {
                            homeConnection = c;
                            Console.WriteLine("homeConnection=" + c.ToString());
                        }
                    }
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                }
            }

            return new NetShare(sharedConnection, homeConnection);
        }

        public static void ShareConnection(INetConnection connectionToShare, INetConnection homeConnection)
        {
            if ((connectionToShare == homeConnection) && (connectionToShare != null))
                throw new ArgumentException("Connections must be different");
            var share = GetCurrentlySharedConnections();
            if (share.SharedConnection != null)
            {
                Console.WriteLine("Disable currently shared connection");
                GetConfiguration(share.SharedConnection).DisableSharing();
            }
            if (share.HomeConnection != null)
                GetConfiguration(share.HomeConnection).DisableSharing();
            if (connectionToShare != null)
            {
                var sc = GetConfiguration(connectionToShare);
                sc.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
            }
            if (homeConnection != null)
            {
                var hc = GetConfiguration(homeConnection);
                bool fw_enabled = hc.InternetFirewallEnabled;
                bool ics_enabled = hc.SharingEnabled;
                Console.WriteLine("fw_enabled=" + fw_enabled + "  ics_enabld=" + ics_enabled);
                if (fw_enabled == false) {
                    hc.EnableInternetFirewall();
                }
                hc.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
            }
        }

        public static INetSharingConfiguration GetConfiguration(INetConnection connection)
        {
            return SharingManager.get_INetSharingConfigurationForINetConnection(connection);
        }

        public static INetConnectionProps GetProperties(INetConnection connection)
        {
            return SharingManager.get_NetConnectionProps(connection);
        }


        public static INetSharingEveryConnectionCollection GetAllConnections()
        {
            return SharingManager.EnumEveryConnection;
        }

        public static INetConnection FindConnectionByIdOrName(string shared)
        {
            return GetConnectionById(shared) ?? GetConnectionByName(shared);
        }

        public static INetConnection GetConnectionById(string guid)
        {
            INetSharingEveryConnectionCollection connections = GetAllConnections();
            foreach (INetConnection c in connections)
            {
                try
                {
                    INetConnectionProps props = GetProperties(c);
                    if (props.Guid == guid)
                        return c;
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // Ignore these  It'ts known that Tunnel adapter isatap   causes getProperties to fail. 
                }
            }
            return null;
        }

        public static INetConnection GetConnectionByName(string name)
        {
            INetSharingEveryConnectionCollection connections = GetAllConnections();
            foreach (INetConnection c in connections)
            {
                try
                {
                    INetConnectionProps props = GetProperties(c);
                    if (props.Name == name)
                        return c;
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // Ignore these  It'ts known that Tunnel adapter isatap   causes getProperties to fail. 
                }
            }
            return null;
        }

    }

    public class NetShare
    {
        public INetConnection SharedConnection;

        public INetConnection HomeConnection;

        public NetShare(INetConnection sharedConnection, INetConnection homeConnection)
        {
            SharedConnection = sharedConnection;
            HomeConnection = homeConnection;
        }

        public bool Exists
        {
            get { return (SharedConnection != null) && (HomeConnection != null); }
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", SharedConnection, HomeConnection);
        }
    }

}
