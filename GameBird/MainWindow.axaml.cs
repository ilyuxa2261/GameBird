using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GameBird;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private Window _Game;
    private void Click_Play(object? sender, RoutedEventArgs e)
    {
        _Game = new Game(); 
        _Game.Show();
        Close();
    }
}