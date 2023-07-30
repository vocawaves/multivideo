using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using MultiVideo.Models;

namespace MultiVideo.Controls;

public class VideoGroupControl : TemplatedControl
{
    public static readonly StyledProperty<Window?> ParentWindowProperty = AvaloniaProperty.Register<VideoGroupControl, Window?>(
        nameof(ParentWindow));

    public Window? ParentWindow
    {
        get => GetValue(ParentWindowProperty);
        set => SetValue(ParentWindowProperty, value);
    }
    
    public static readonly StyledProperty<VideoGroup?> VideoGroupSourceProperty =
        AvaloniaProperty.Register<VideoGroupControl, VideoGroup?>(
            nameof(VideoGroupSource));

    public VideoGroup? VideoGroupSource
    {
        get => GetValue(VideoGroupSourceProperty);
        set => SetValue(VideoGroupSourceProperty, value);
    }
}