using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using MultiVideo.Views;

namespace MultiVideo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly LibVLC _mainLibVlc;
    private readonly LibVLC _lyricLibVlc;

    [ObservableProperty] 
    private MediaPlayer? _mainPlayer;

    [ObservableProperty] 
    private MediaPlayer? _lyricPlayer;

    [ObservableProperty] 
    private string? _mainVideoPath = @"C:\Users\Sekoree\Desktop\Sweet Magic.mp4";

    [ObservableProperty] 
    private string? _lyricVideoPath = @"C:\Users\Sekoree\Desktop\[LYRIC] Sweet Magic.mp4";
    
    [ObservableProperty]
    private float _mainPosition = 0f;
    
    [ObservableProperty]
    private float _lyricPosition = 0f;

    public MainViewModel()
    {
        //Init LibVLC
        Core.Initialize();

        _mainLibVlc = new LibVLC();
        _lyricLibVlc = new LibVLC(/*"--no-audio"*/);
        
        //Init MediaPlayers
        MainPlayer = new MediaPlayer(_mainLibVlc);
        LyricPlayer = new MediaPlayer(_lyricLibVlc);
        //LyricPlayer.Volume = 0;
        
        MainPlayer.PositionChanged += (sender, args) => Dispatcher.UIThread.Invoke(() => MainPosition = args.Position);
        LyricPlayer.PositionChanged += (sender, args) => Dispatcher.UIThread.Invoke(() => LyricPosition = args.Position);
    }

    [RelayCommand]
    public void OpenMainVideoView()
    {
        var wnd = new MainVideoWindow()
        {
            DataContext = this
        };
        wnd.KeyUp += (sender, args) =>
        {
            if (args.Key != Avalonia.Input.Key.F11) 
                return;
            wnd.WindowState = wnd.WindowState == Avalonia.Controls.WindowState.FullScreen ? Avalonia.Controls.WindowState.Normal : Avalonia.Controls.WindowState.FullScreen;
        };
        wnd.Show();
    }

    [RelayCommand]
    public void OpenLyricVideoView()
    {
        var wnd = new LyricVideoWindow()
        {
            DataContext = this
        };
        
        wnd.KeyUp += (sender, args) =>
        {
            if (args.Key != Avalonia.Input.Key.F11) 
                return;
            wnd.WindowState = wnd.WindowState == Avalonia.Controls.WindowState.FullScreen ? Avalonia.Controls.WindowState.Normal : Avalonia.Controls.WindowState.FullScreen;
        };
        wnd.Show();
    }
    
    [RelayCommand]
    public void PlayVideos()
    {
        var mainVideo = new Media(_mainLibVlc, MainVideoPath!);
        var lyricVideo = new Media(_lyricLibVlc, LyricVideoPath!);
        
        MainPlayer!.Media = mainVideo;
        LyricPlayer!.Media = lyricVideo;
        
        MainPlayer.Play();
        LyricPlayer.Play();
    }
}