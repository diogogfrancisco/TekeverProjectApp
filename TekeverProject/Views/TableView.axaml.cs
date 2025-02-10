using Avalonia.Controls;
using System.Diagnostics;
using TekeverProject.ViewModels;

namespace TekeverProject.Views
{
    public partial class TableView : UserControl
    {
        public TableView()
        {
            InitializeComponent();
        }

        public TableView(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = new TableViewModel();
        }
    }
}