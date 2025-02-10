using Avalonia.Controls;
using TekeverProject.ViewModels;

namespace TekeverProject.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
