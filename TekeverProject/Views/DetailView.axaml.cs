using Avalonia.Controls;
using TekeverProject.Models;
using TekeverProject.ViewModels;

namespace TekeverProject.Views
{
    public partial class DetailView : UserControl
    {
        public DetailView()
        {
            InitializeComponent();
        }

        public DetailView(NetworkInterfaceItem item, MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = new DetailViewModel(item, viewModel);
        }
    }
}