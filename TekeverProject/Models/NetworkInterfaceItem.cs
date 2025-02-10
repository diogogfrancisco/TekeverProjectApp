using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TekeverProject.Models
{
    public class NetworkInterfaceItem : ObservableObject
    {
        private NetworkInterface _networkInterface;

        public NetworkInterface NetworkInterface
        {
            get { return _networkInterface; }
            set { SetProperty(ref _networkInterface, value); }
        }
        public string Name
        {
            get
            {
                if (_networkInterface?.Name != null)
                    return _networkInterface.Name;
                else
                    return "-";
            }
        }

        public string NetworkInterfaceType
        {
            get
            {
                if (_networkInterface?.NetworkInterfaceType != null)
                    return _networkInterface.NetworkInterfaceType.ToString();
                else
                    return "-";
            }
        }

        public string OperationalStatus
        {
            get
            {
                if (_networkInterface?.OperationalStatus != null)
                    return _networkInterface.OperationalStatus.ToString();
                else
                    return "-";
            }
        }

        public string MAC
        {
            get 
            {
                if (_networkInterface?.GetPhysicalAddress() != null 
                        && String.IsNullOrEmpty(_networkInterface.GetPhysicalAddress().ToString()) == false)
                {
                    string mac = _networkInterface.GetPhysicalAddress().ToString();
                    return Regex.Replace(mac, "(.{2})", "$1-").TrimEnd('-');
                }
                else
                    return "-";
            }
        }

        public ObservableCollection<IPAddressInfo> IPAddresses
        {
            get
            {
                var addresses = new ObservableCollection<IPAddressInfo>();
                if (_networkInterface != null)
                {
                    foreach (var addr in _networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        string type = addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "IPv4" : "IPv6";
                        addresses.Add(new IPAddressInfo(addr.Address.ToString(), type, addr.IPv4Mask));
                    }
                }
                return addresses;
            }
        }

        public string Gateway
        {
            get
            {
                var gateway = _networkInterface.GetIPProperties().GatewayAddresses.FirstOrDefault();
                return gateway != null ? gateway.Address.ToString() : "Not Set";
            }
        }

        public List<string> DNSAddresses
        {
            get
            {
                return _networkInterface.GetIPProperties().DnsAddresses
                    .Select(addr => addr.ToString())
                    .ToList();
            }
        }

        public NetworkInterfaceItem(NetworkInterface networkInterface) => NetworkInterface = networkInterface;
    }

    public class IPAddressInfo
    {
        public string Address { get; set; }
        public string Type { get; set; }
        public string IPv4Mask { get; set; }
        public bool CanDelete => Type == "IPv4";

        public IPAddressInfo(string address, string type, IPAddress ipv4Mask)
        {
            Address = address;
            Type = type;
            IPv4Mask = Type == "IPv4" ? ipv4Mask.ToString() : String.Empty;
        }
    }
}
