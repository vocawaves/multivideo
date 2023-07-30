using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MultiVideo.Views;

public partial class ReorderListWindow : Window
{
    public ReorderListWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}