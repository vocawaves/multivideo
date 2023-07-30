using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace MultiVideo.Views;

public partial class VideoGroupEditWindow : Window
{
    public VideoGroupEditWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        BeginMoveDrag(e);
    }

    private async void Secondary_Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var fpo = new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Select Video File",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video File")
                {
                    MimeTypes = new[] { "video/*" },
                    Patterns = new[] { "*.mp4" }
                }
            }
        };

        var tl = TopLevel.GetTopLevel(this);
        if (tl is not Window window)
            return;

        var files = await window.StorageProvider.OpenFilePickerAsync(fpo);
        if (files.Count == 0)
            return;
        
        SecondaryPath.Text = files[0].TryGetLocalPath();
    }

    private async void Main_Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var fpo = new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Select Video File",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video File")
                {
                    MimeTypes = new[] { "video/*" },
                    Patterns = new[] { "*.mp4" }
                }
            }
        };

        var tl = TopLevel.GetTopLevel(this);
        if (tl is not Window window)
            return;

        var files = await window.StorageProvider.OpenFilePickerAsync(fpo);
        if (files.Count == 0)
            return;
        
        MainPath.Text = files[0].TryGetLocalPath();
    }
}