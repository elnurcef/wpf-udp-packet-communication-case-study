using System.Windows;
using Baykar.SimulationInterface.ViewModels;

namespace Baykar.SimulationInterface;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
