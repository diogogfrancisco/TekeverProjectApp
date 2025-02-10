using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using TekeverProject.Models;

namespace TekeverProject.ViewModels
{
    public class TableViewModel : ViewModelBase
    {
        public ObservableCollection<NetworkInterfaceItem> NetworkInterfaceItems { get; }

        public TableViewModel()
        {
            NetworkInterfaceItems = new ObservableCollection<NetworkInterfaceItem>(
                NetworkInterface.GetAllNetworkInterfaces()
                .Select(ni => new NetworkInterfaceItem(ni))
            );
        }
    }
}