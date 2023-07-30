using System;
using Avalonia.Controls;

namespace MultiVideo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var dc = DataContext as IDisposable;
        dc?.Dispose();
        base.OnClosing(e);
    }
}