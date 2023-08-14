using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiVideo.Models;

public partial class VideoGroup : ObservableObject
{
    [ObservableProperty]
    private string _title = "Untitled";
    [ObservableProperty]
    private string? _audioVideoPath;
    [ObservableProperty]
    private string? _nonAudioVideoPath;
    [ObservableProperty]
    private TimeSpan _audioVideoStartDelay = TimeSpan.Zero;
    [ObservableProperty]
    private TimeSpan _nonAudioVideoStartDelay = TimeSpan.Zero;
    [ObservableProperty]
    private bool _nonAudioOnMainScreen = false;
    [ObservableProperty]
    private bool _waitForBothVideosToFinish = false;

    
    [ObservableProperty] 
    private Bitmap? _thumbnail;
    
    public bool IsMainDelayed => (AudioVideoStartDelay > NonAudioVideoStartDelay);

    public VideoGroup(
        string? audioVideoPath, 
        string? nonAudioVideoPath, 
        string? title = null, 
        TimeSpan? audioVideoDelay = null, 
        TimeSpan? nonAudioVideoDelay = null, 
        bool useNonAudioOnMainScreen = false,
        bool waitForBothVideosToFinish = false,
        Bitmap? thumbnail = null)
    {
        AudioVideoPath = audioVideoPath;
        NonAudioVideoPath = nonAudioVideoPath;
        if (title is not null)
            Title = title;

        if (audioVideoDelay is not null)
            AudioVideoStartDelay = audioVideoDelay.GetValueOrDefault();

        if (nonAudioVideoDelay is not null)
            NonAudioVideoStartDelay = nonAudioVideoDelay.GetValueOrDefault();

        NonAudioOnMainScreen = useNonAudioOnMainScreen;
        WaitForBothVideosToFinish = waitForBothVideosToFinish;
        
        if (thumbnail is not null)
            Thumbnail = thumbnail;
    }
}