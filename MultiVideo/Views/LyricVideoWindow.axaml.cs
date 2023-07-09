using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MultiVideo.Views;

public partial class LyricVideoWindow : Window
{
    public LyricVideoWindow()
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