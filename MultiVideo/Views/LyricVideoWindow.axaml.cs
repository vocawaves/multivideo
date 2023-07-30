using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace MultiVideo.Views;

public partial class LyricVideoWindow : Window
{
    public LyricVideoWindow()
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
                case Key.F10:
                    // toggle borderless
                    if (ExtendClientAreaToDecorationsHint == true)
                    {
                        ExtendClientAreaToDecorationsHint = false;
                        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
                        ExtendClientAreaTitleBarHeightHint = -1;
                    }
                    else
                    {
                        ExtendClientAreaToDecorationsHint = true;
                        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
                        ExtendClientAreaTitleBarHeightHint = -1;
                    }
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