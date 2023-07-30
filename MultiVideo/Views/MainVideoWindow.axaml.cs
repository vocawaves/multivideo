using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace MultiVideo.Views;

public partial class MainVideoWindow : Window
{
    public MainVideoWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        this.KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.F11:
                    WindowState = WindowState == WindowState.Normal
                        ? WindowState.FullScreen
                        : WindowState.Normal;
                    break;
            }
        };
        this.PointerPressed += (_, e) => BeginMoveDrag(e);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}