using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiVideo.Models;

public partial class VideoGroup : ObservableObject
{
    [ObservableProperty]
    private string _title = "Untitled";
    [ObservableProperty]
    private string _mainVideoPath;
    [ObservableProperty]
    private string _secondaryVideoPath;
    [ObservableProperty]
    private TimeSpan _mainVideoStartDelay = TimeSpan.Zero;
    [ObservableProperty]
    private TimeSpan _secondaryVideoStartDelay = TimeSpan.Zero;

    [ObservableProperty] 
    private Bitmap? _thumbnail;

    public VideoGroup(
        string mainVideoPath, 
        string secondaryVideoPath, 
        string? title = null, 
        TimeSpan? mainDelay = null, 
        TimeSpan? secondaryDelay = null, 
        Bitmap? thumbnail = null)
    {
        MainVideoPath = mainVideoPath;
        SecondaryVideoPath = secondaryVideoPath;
        if (title is not null)
            Title = title;

        if (mainDelay is not null)
            MainVideoStartDelay = mainDelay.GetValueOrDefault();

        if (secondaryDelay is not null)
            SecondaryVideoStartDelay = secondaryDelay.GetValueOrDefault();

        if (thumbnail is not null)
            Thumbnail = thumbnail;
    }
}