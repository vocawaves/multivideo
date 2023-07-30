using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using MultiVideo.Models;
using MultiVideo.Views;

namespace MultiVideo.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private INotificationManager? _notificationManager;

    private INotificationManager NotificationManager => _notificationManager ??=
        new WindowNotificationManager(TopLevel.GetTopLevel(
            ((IClassicDesktopStyleApplicationLifetime)Application
                .Current?.ApplicationLifetime!).MainWindow));

    private readonly SavableVideoGroupContext _saveCtx = new SavableVideoGroupContext(new JsonSerializerOptions()
    {
        WriteIndented = true
    });

    private readonly LibVLC _mainLibVlc;
    private readonly LibVLC _lyricLibVlc;


    [ObservableProperty] private MediaPlayer _mainPlayer;

    [ObservableProperty] private MediaPlayer _lyricPlayer;

    [ObservableProperty] private Media? _mainMedia;

    [ObservableProperty] private Media? _lyricMedia;

    [ObservableProperty] private ObservableCollection<GroupWrapper> _videoGroups = new();

    #region Playback Controls

    [ObservableProperty] private bool _isPlaying = false;

    [ObservableProperty] private int _volume = 100;

    [ObservableProperty] private TimeSpan _mainPosition = TimeSpan.Zero;

    [ObservableProperty] private float _mainRealPosition = 0f;

    [ObservableProperty] private GroupWrapper? _currentPlayingGroup;

    #endregion

    #region Video Windows

    [ObservableProperty] private bool _isMainVideoWindowClosed = true;
    private Window? _mainVideoWindow = null;
    [ObservableProperty] private bool _isLyricVideoWindowClosed = true;
    private Window? _lyricVideoWindow = null;

    #endregion

    public MainViewModel()
    {
        //Init LibVLC
        Core.Initialize();
        
        string[] mainOptions = new string[]
        {
            "--http-port=8080",
            "--http-password=kaito"
        };
        
        string[] lyricOptions = new string[]
        {
            "--http-port=8081",
            "--http-password=kaito",
            "--no-audio"
        };

        _mainLibVlc = new LibVLC(mainOptions);
        _lyricLibVlc = new LibVLC(lyricOptions);
        
        // funky http for Numark
        try
        {
            _mainLibVlc.AddInterface("http");
            _lyricLibVlc.AddInterface("http");
        }
        catch
        {
            // ignored
        }
        
        //Init MediaPlayers
        MainPlayer = new MediaPlayer(_mainLibVlc);
        LyricPlayer = new MediaPlayer(_lyricLibVlc);
        //LyricPlayer.SetAudioOutput("adummy"); //basically mute
        
        MainPlayer.EndReached += MainPlayerOnEndReached;
        MainPlayer.PositionChanged += MainPlayerOnPositionChanged;
    }

    private void MainPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        MainPosition = TimeSpan.FromMilliseconds(e.Position * MainPlayer.Length);
        MainRealPosition = e.Position;
    }

    private void MainPlayerOnEndReached(object? sender, EventArgs e)
    {
        if (CurrentPlayingGroup is null)
            return; //idk but yeah
        if (!IsPlaying)
            return; //stopped?

        var indexOfCurrent = VideoGroups.IndexOf(CurrentPlayingGroup);
        if (indexOfCurrent == VideoGroups.Count - 1)
        {
            Stop();
            return;
        }

        _ = Dispatcher.UIThread.InvokeAsync(async () => await PlayFromBlock(VideoGroups[indexOfCurrent + 1]));
    }

    partial void OnVolumeChanged(int value)
    {
        MainPlayer.Volume = value;
    }

    #region Playback Commands

    [RelayCommand]
    private void Stop()
    {
        IsPlaying = false;
        MainPlayer.Stop();
        LyricPlayer.Stop();
        MainMedia = null;
        LyricMedia = null;
        if (CurrentPlayingGroup is null)
            return;
        CurrentPlayingGroup.IsPlaying = false;
        CurrentPlayingGroup = null;
    }

    [RelayCommand]
    private async Task PlayFromBlock(GroupWrapper group)
    {
        Stop(); //Just in case

        if (IsMainVideoWindowClosed || IsLyricVideoWindowClosed)
        {
            this.NotificationManager.Show(new Notification("Error",
                "Please open both video windows before playing.", NotificationType.Error));
            return;
        }

        MainMedia = !string.IsNullOrEmpty(group.VideoGroup.MainVideoPath)
            ? new Media(_mainLibVlc, group.VideoGroup.MainVideoPath)
            : null;
        if (MainMedia is not null)
            await MainMedia.Parse();
        LyricMedia = !string.IsNullOrEmpty(group.VideoGroup.SecondaryVideoPath)
            ? new Media(_lyricLibVlc, group.VideoGroup.SecondaryVideoPath)
            : null;
        if (LyricMedia is not null)
            await LyricMedia.Parse();

        MainPlayer.Media = MainMedia;
        MainPlayer.Play();
        MainPlayer.Pause();
        MainPlayer.Position = 0f;
        LyricPlayer.Media = LyricMedia;
        LyricPlayer.Play();
        LyricPlayer.Pause();
        LyricPlayer.Position = 0f;

        var waitForMain = group.VideoGroup.MainVideoStartDelay > group.VideoGroup.SecondaryVideoStartDelay;
        var actualWaitTime = waitForMain
            ? (group.VideoGroup.MainVideoStartDelay - group.VideoGroup.SecondaryVideoStartDelay)
            : (group.VideoGroup.SecondaryVideoStartDelay - group.VideoGroup.MainVideoStartDelay);

        //High speed case
        if (group.VideoGroup.MainVideoStartDelay == TimeSpan.Zero &&
            group.VideoGroup.SecondaryVideoStartDelay == TimeSpan.Zero)
        {
            MainPlayer.Play();
            LyricPlayer.Play();
        }
        else if (waitForMain)
        {
            LyricPlayer.Play();
            await Task.Delay(actualWaitTime);
            MainPlayer.Play();
        }
        else
        {
            MainPlayer.Play();
            await Task.Delay(actualWaitTime);
            LyricPlayer.Play();
        }

        IsPlaying = true;
        group.IsPlaying = true;
        CurrentPlayingGroup = group;
    }

    [RelayCommand]
    private async Task PlayFromButton()
    {
        //Pause
        if (MainPlayer.State == VLCState.Playing || LyricPlayer.State == VLCState.Playing)
        {
            MainPlayer.Pause();
            LyricPlayer.Pause();
        }
        else if (MainPlayer.State == VLCState.Paused || LyricPlayer.State == VLCState.Paused)
        {
            MainPlayer.Play();
            LyricPlayer.Play();
        }
        else
        {
            var group = VideoGroups.FirstOrDefault();
            if (group is null)
            {
                NotificationManager.Show(new Notification(
                    "No video groups found",
                    "Please add a video group before playing",
                    NotificationType.Warning));
                return;
            }

            await PlayFromBlock(group);
        }
    }

    [RelayCommand]
    private async Task Rewind()
    {
        var index = CurrentPlayingGroup is null ? 0 : VideoGroups.IndexOf(CurrentPlayingGroup);
        if (index == 0)
        {
            NotificationManager.Show(new Notification(
                "Cannot rewind",
                "You are already at the beginning of the playlist",
                NotificationType.Warning));
            return;
        }

        await PlayFromBlock(VideoGroups[index - 1]);
    }

    [RelayCommand]
    private async Task FastForward()
    {
        if (VideoGroups.Count == 0)
        {
            NotificationManager.Show(new Notification(
                "Cannot fast forward",
                "You have no video groups",
                NotificationType.Warning));
            return;
        }
        
        var index = CurrentPlayingGroup is null ? 0 : VideoGroups.IndexOf(CurrentPlayingGroup);
        if (index == VideoGroups.Count - 1)
        {
            NotificationManager.Show(new Notification(
                "Cannot fast forward",
                "You are already at the end of the playlist",
                NotificationType.Warning));
            return;
        }

        await PlayFromBlock(VideoGroups[index + 1]);
    }

    #endregion

    #region Player Windows

    [RelayCommand(CanExecute = nameof(IsMainVideoWindowClosed))]
    private void OpenMainVideoWindow()
    {
        _mainVideoWindow = new MainVideoWindow()
        {
            DataContext = this
        };
        _mainVideoWindow.Closed += (_, _) => IsMainVideoWindowClosed = true;
        _mainVideoWindow.Show();
        IsMainVideoWindowClosed = false;
    }

    [RelayCommand(CanExecute = nameof(IsLyricVideoWindowClosed))]
    private void OpenLyricVideoWindow()
    {
        _lyricVideoWindow = new LyricVideoWindow()
        {
            DataContext = this
        };
        _lyricVideoWindow.Closed += (_, _) => IsLyricVideoWindowClosed = true;
        _lyricVideoWindow.Show();
        IsLyricVideoWindowClosed = false;
    }
    
    [RelayCommand]
    private async Task OpenReorderWindow(Window parent)
    {
        var reorderWindow = new ReorderListWindow()
        {
            DataContext = this
        };
        await reorderWindow.ShowDialog(parent);
    }

    #endregion

    #region Video Groups

    [RelayCommand]
    private async Task AddVideoGroup(Window parent)
    {
        var group = new VideoGroup(string.Empty, string.Empty, "New Video Group");
        var groupWrapper = new GroupWrapper
        {
            VideoGroup = group
        };
        var editWindow = new VideoGroupEditWindow
        {
            DataContext = new VideoGroupEditViewModel
            {
                Group = groupWrapper
            }
        };
        var result = await editWindow.ShowDialog<GroupWrapper?>(parent);
        if (result is null)
            return;

        VideoGroups.Add(result);
    }

    [RelayCommand]
    private void ClearVideoGroups()
    {
        VideoGroups.Clear();
    }

    #endregion

    #region File Menu

    [RelayCommand]
    private async Task SaveVideoGroups(Window parent)
    {
        if (VideoGroups.Count == 0)
            return;

        var sfo = new FilePickerSaveOptions()
        {
            Title = "Save Video Groups",
            DefaultExtension = "mvc",
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("MultiVideo Configuration")
                {
                    MimeTypes = new[] { "application/json" },
                    Patterns = new[] { "*.mvc" }
                }
            }
        };

        var savePath = await parent.StorageProvider.SaveFilePickerAsync(sfo);
        if (savePath is null)
            return;

        var savableGroups = VideoGroups.Select(x => SavableVideoGroup.FromVideoGroup(x.VideoGroup));
        var json = JsonSerializer.Serialize(savableGroups, _saveCtx.IEnumerableSavableVideoGroup);
        var path = savePath.TryGetLocalPath();
        if (path is null)
            return;

        await File.WriteAllTextAsync(path, json);
    }

    [RelayCommand]
    private async Task LoadVideoGroups(Window parent)
    {
        var ofo = new FilePickerOpenOptions()
        {
            Title = "Load Video Groups",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("MultiVideo Configuration")
                {
                    MimeTypes = new[] { "application/json" },
                    Patterns = new[] { "*.mvc" }
                }
            }
        };

        var files = await parent.StorageProvider.OpenFilePickerAsync(ofo);
        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (path is null)
            return;

        var json = await File.ReadAllTextAsync(path);
        var savableGroups = JsonSerializer.Deserialize(json, _saveCtx.SavableVideoGroupArray);
        if (savableGroups is null)
            return;

        var groups = savableGroups.Select(x => x.ToVideoGroup());
        VideoGroups.Clear();
        foreach (var group in groups)
        {
            VideoGroups.Add(new GroupWrapper
            {
                VideoGroup = group,
                IsInitial = false
            });
        }
    }

    #endregion

    public void Dispose()
    {
        Stop();
        _mainVideoWindow?.Close();
        _lyricVideoWindow?.Close();
        MainPlayer?.Dispose();
        LyricPlayer?.Dispose();
        _mainLibVlc.Dispose();
        _lyricLibVlc.Dispose();
    }
}