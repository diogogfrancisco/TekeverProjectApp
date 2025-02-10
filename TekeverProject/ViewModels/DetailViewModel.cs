using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TekeverProject.Models;
using TekeverProject.Views;
using System.Management;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.RegularExpressions;
using System.Reflection.Metadata.Ecma335;

namespace TekeverProject.ViewModels
{
    public partial class DetailViewModel : ViewModelBase
    {
        private MainViewModel _mainViewModel;

        public bool IsInterfaceEditable => SelectedItem.NetworkInterface.NetworkInterfaceType switch
        {
            NetworkInterfaceType.Ethernet or
            NetworkInterfaceType.GigabitEthernet or
            NetworkInterfaceType.FastEthernetT or
            NetworkInterfaceType.FastEthernetFx or
            NetworkInterfaceType.Wireless80211 or
            NetworkInterfaceType.Isdn or
            NetworkInterfaceType.BasicIsdn or
            NetworkInterfaceType.PrimaryIsdn => true,

            _ => false //Loopback, MobileBroadbandGsm, Unknown, IpOverAtm, etc
        };

        #region Watermarks for texboxes

        [ObservableProperty]
        private string _gatewayWatermark = "Enter gateway...";

        [ObservableProperty]
        private string _dNS1Watermark = "Enter DNS...";
        
        [ObservableProperty]
        private string _dNS2Watermark = "Enter DNS...";
        
        [ObservableProperty]
        private string _newIPv4Watermark = "Enter IPv4...";
        
        [ObservableProperty]
        private string _newIPv4SubnetMaskWatermark = "Enter Subnet Mask...";
        
        [ObservableProperty]
        private string _replaceIPv4Watermark = "Select IPv4...";
        
        [ObservableProperty]
        private string _replaceIPv4SubnetMaskWatermark = "Select IPv4...";

        #endregion

        #region Commands

        public ICommand GoBackToTableCommand { get; }
        public ICommand ReloadPageCommand { get; }
        public ICommand ApplyGatewayCommand { get; }
        public ICommand ApplyDNSCommand { get; }
        public ICommand AddIPv4Command { get; }
        public ICommand DeleteIPv4Command { get; }
        public ICommand ReplaceIPv4Command => new RelayCommand(ReplaceIPv4, () => CanReplaceIPv4);

        #endregion

        public NetworkInterfaceItem SelectedItem { get; }
        
        public ObservableCollection<IPAddressInfo> IPAddresses => SelectedItem.IPAddresses;

        [ObservableProperty]
        private string _gateway;

        [ObservableProperty]
        private string _dNS1;

        [ObservableProperty]
        private string _dNS2;

        [ObservableProperty]
        private string _newIPv4Address;

        [ObservableProperty]
        private string _newIPv4SubnetMask;


        private string _replaceIPv4Address;
        public string ReplaceIPv4Address
        {
            get => _replaceIPv4Address;
            set
            {
                SetProperty(ref _replaceIPv4Address, value);
                OnPropertyChanged(nameof(CanReplaceIPv4));
                OnPropertyChanged(nameof(ReplaceIPv4Command));
            }
        }

        private string _replaceIPv4SubnetMask;
        public string ReplaceIPv4SubnetMask
        {
            get => _replaceIPv4SubnetMask;
            set
            {
                SetProperty(ref _replaceIPv4SubnetMask, value);
                OnPropertyChanged(nameof(CanReplaceIPv4));
                OnPropertyChanged(nameof(ReplaceIPv4Command));
            }
        }

        private IPAddressInfo _selectedIPAddress;
        public IPAddressInfo SelectedIPAddress
        {
            get => _selectedIPAddress;
            set
            {
                if (SetProperty(ref _selectedIPAddress, value))
                {
                    if (_selectedIPAddress != null && _selectedIPAddress.Type == "IPv4")
                    {
                        ReplaceIPv4Address = _selectedIPAddress.Address;
                        ReplaceIPv4SubnetMask = _selectedIPAddress.IPv4Mask;
                    }

                    OnPropertyChanged(nameof(CanReplaceIPv4));
                    OnPropertyChanged(nameof(ReplaceIPv4Command));
                }
            }
        }

        public bool CanReplaceIPv4 => SelectedIPAddress != null && SelectedIPAddress.Type == "IPv4" && 
                                            (SelectedIPAddress.Address != ReplaceIPv4Address || SelectedIPAddress.IPv4Mask != ReplaceIPv4SubnetMask);

        public DetailViewModel(NetworkInterfaceItem item, MainViewModel viewModel)
        {
            _mainViewModel = viewModel;

            NetworkInterface updatedNetworkInterface = GetNetworkInterfaceByName(item.Name);
            SelectedItem = new NetworkInterfaceItem(updatedNetworkInterface);
            Gateway = SelectedItem.Gateway;
            DNS1 = SelectedItem.DNSAddresses.FirstOrDefault() ?? string.Empty;
            DNS2 = SelectedItem.DNSAddresses.Skip(1).FirstOrDefault() ?? string.Empty;

            ApplyGatewayCommand = new RelayCommand(ApplyGateway);
            ApplyDNSCommand = new RelayCommand(ApplyDNS);
            AddIPv4Command = new RelayCommand(AddIPv4);
            DeleteIPv4Command = new RelayCommand<string>(DeleteIPv4);
            GoBackToTableCommand = new RelayCommand(viewModel.GoBackToTable);
            ReloadPageCommand = new RelayCommand(RefreshView);
        }

        static NetworkInterface GetNetworkInterfaceByName(string name)
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    return ni;
                }
            }
            return null;
        }

        private void ApplyGateway()
        {
            if (!NetworkConfig.IsValidGateway(Gateway))
            {
                GatewayWatermark = "Invalid Gateway!";
                Gateway = "";
                return;
            }
            try
            {
                bool success = NetworkConfig.SetGateway(SelectedItem.Name, Gateway);
                if (success)
                {
                    RefreshView();
                }
                else
                {
                    GatewayWatermark = "Operation failed!";
                    Gateway = "";
                    return;
                }
            }
            catch (Exception)
            {
                GatewayWatermark = "Operation failed!";
                Gateway = "";
            }
        }

        private void ApplyDNS()
        {
            bool validDNS1 = NetworkConfig.IsValidDNS(DNS1);
            bool validDNS2 = NetworkConfig.IsValidDNS(DNS2);
            if (!validDNS1)
            {
                DNS1Watermark = "Invalid DNS!";
                DNS1 = "";
            }
            if (!validDNS2)
            {
                DNS2Watermark = "Invalid DNS!";
                DNS2 = "";
            }
            if (!validDNS1 || !validDNS2)
                return;

            try
            {
                string[] dnsServers = !string.IsNullOrWhiteSpace(DNS2) ? new[] { DNS1, DNS2 } : new[] { DNS1 };
                bool success = NetworkConfig.SetDNS(SelectedItem.Name, dnsServers);
                if (success)
                    RefreshView();
                else
                {
                    DNS1Watermark = DNS2Watermark = "Operation failed!";
                    DNS1 = DNS2 = "";                    
                }
            }
            catch (Exception)
            {
                DNS1Watermark = DNS2Watermark = "Operation failed!";
                DNS1 = DNS2 = "";
            }
        }

        private void AddIPv4()
        {
            bool validNewIPv4Address = NetworkConfig.IsValidIPv4(NewIPv4Address);
            bool validNewIPv4SubnetMask = NetworkConfig.IsValidSubnetMask(NewIPv4SubnetMask);
            if (!validNewIPv4Address)
            {
                NewIPv4Watermark = "Invalid IPv4!";
                NewIPv4Address = "";
            }
            if (!validNewIPv4SubnetMask)
            {
                NewIPv4SubnetMaskWatermark = "Invalid Subnet Mask!";
                NewIPv4SubnetMask = "";
            }
            if (!validNewIPv4Address || !validNewIPv4SubnetMask)
                return;

            bool success = NetworkConfig.AddIPv4ToInterface(SelectedItem.Name, NewIPv4Address, NewIPv4SubnetMask);
            if (success)
                RefreshView();
            else
            {
                NewIPv4Watermark = NewIPv4SubnetMaskWatermark = "Operation failed!";
                NewIPv4Address = NewIPv4SubnetMask = "";
                return;
            }  
        }

        private void DeleteIPv4(string ipAddress)
        {
            if (!NetworkConfig.IsValidIPv4(ipAddress))
                return;

            //Prevent last IPv4 from being deleted
            int ipv4Count = IPAddresses.Count(ip => ip.Type == "IPv4");
            if (ipv4Count == 1)
                return;

            bool success = NetworkConfig.DeleteIPv4ToInterface(SelectedItem.Name, ipAddress);
            if (success)
                RefreshView();
        }

        private void ReplaceIPv4()
        {
            //Prevents trying to replace an existing IP with the same IP and Subnet Mask
            if (SelectedIPAddress == null || (SelectedIPAddress.Address == ReplaceIPv4Address && SelectedIPAddress.IPv4Mask == ReplaceIPv4SubnetMask) )
                return;

            bool validReplaceIPv4Address = NetworkConfig.IsValidIPv4(ReplaceIPv4Address);
            bool validReplaceIPv4SubnetMask = NetworkConfig.IsValidSubnetMask(ReplaceIPv4SubnetMask);
            if (!validReplaceIPv4Address)
            {
                ReplaceIPv4Watermark = "Invalid IPv4!";
                ReplaceIPv4Address = "";
            }
            if (!validReplaceIPv4SubnetMask)
            {
                ReplaceIPv4SubnetMaskWatermark = "Invalid Subnet Mask!";
                ReplaceIPv4SubnetMask = "";
            }
            if (!validReplaceIPv4Address || !validReplaceIPv4SubnetMask)
                return;
            
            bool addSuccess = NetworkConfig.AddIPv4ToInterface(SelectedItem.Name, ReplaceIPv4Address, ReplaceIPv4SubnetMask);
            if (!addSuccess)
            {
                ReplaceIPv4Watermark = ReplaceIPv4SubnetMaskWatermark = "Operation failed!";
                ReplaceIPv4Address = ReplaceIPv4SubnetMask = "";
                return;
            }
            else
            {
                string oldIpAddress = SelectedIPAddress.Address;
                bool deleteSuccess = NetworkConfig.DeleteIPv4ToInterface(SelectedItem.Name, oldIpAddress);
                if (!deleteSuccess)
                {
                    ReplaceIPv4Watermark = ReplaceIPv4SubnetMaskWatermark = "Operation failed!";
                    ReplaceIPv4Address = ReplaceIPv4SubnetMask = "";
                }
            }

            RefreshView();
        }

        private void RefreshView()
        {
            _mainViewModel.CurrentView = new DetailView(new NetworkInterfaceItem(SelectedItem.NetworkInterface), _mainViewModel);
        }
    }
}