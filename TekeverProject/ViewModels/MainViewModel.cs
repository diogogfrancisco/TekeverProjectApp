using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TekeverProject.Models;
using TekeverProject.Views;

namespace TekeverProject.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand ShowDetailsCommand { get; }
        public ICommand GoBackToTableCommand { get; }

        public MainViewModel()
        {
            CurrentView = new TableView(this);

            ShowDetailsCommand = new RelayCommand<NetworkInterfaceItem>(ShowDetails);
            GoBackToTableCommand = new RelayCommand(GoBackToTable);
        }

        private void ShowDetails(NetworkInterfaceItem item)
        {
            if (item != null)
            {
                CurrentView = new DetailView(item, this);
            }
        }

        public void GoBackToTable()
        {
            CurrentView = new TableView(this);
        }
    }
}