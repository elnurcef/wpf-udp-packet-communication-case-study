using System.Windows;
using Baykar.UserInterface.ViewModels;

namespace Baykar.UserInterface;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
