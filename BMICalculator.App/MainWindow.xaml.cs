using System.Windows;
using BMICalculator.App.ViewModels;

namespace BMICalculator.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
