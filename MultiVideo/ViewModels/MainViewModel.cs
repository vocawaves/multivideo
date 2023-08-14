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

    private readonly SavableVideoGroupContext _saveCtx = new(new JsonSerializerOptions()
    {
        WriteIndented = true
    });

    private readonly LibVLC _audioLibVlc;
    private readonly LibVLC _noAudioLibVlc;


    [ObservableProperty] private MediaPlayer _audioPlayer;

    [ObservableProperty] private MediaPlayer _noAudioPlayer;
    private bool arePlayersSwitched = false;

    [ObservableProperty] private ObservableCollection<GroupWrapper> _videoGroups = new();

    #region Playback Controls

    [ObservableProperty] private bool _isPlaying = false;

    [ObservableProperty] private int _volume = 100;

    [ObservableProperty] private PlayQueueItem? _currentPlayingGroup;
    public float AudioPlayerPos => arePlayersSwitched ? NoAudioPlayer.Position : AudioPlayer.Position;
    public float NoAudioPlayerPos => !arePlayersSwitched ? NoAudioPlayer.Position : AudioPlayer.Position;

    public TimeSpan AudioPlayerTime => arePlayersSwitched
        ? TimeSpan.FromMilliseconds(NoAudioPlayer.Time)
        : TimeSpan.FromMilliseconds(AudioPlayer.Time);

    public TimeSpan NoAudioPlayerTime => !arePlayersSwitched
        ? TimeSpan.FromMilliseconds(NoAudioPlayer.Time)
        : TimeSpan.FromMilliseconds(AudioPlayer.Time);

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

        _audioLibVlc = new LibVLC();
        _noAudioLibVlc = new LibVLC("--no-audio");

        AudioPlayer = new MediaPlayer(_audioLibVlc);
        NoAudioPlayer = new MediaPlayer(_noAudioLibVlc);

        AudioPlayer.EndReached += AudioPlayerOnEndReached;
        NoAudioPlayer.EndReached += NonAudioPlayerOnEndReached;

        AudioPlayer.PositionChanged += (_, _) => OnPropertyChanged(nameof(AudioPlayerPos));
        AudioPlayer.TimeChanged += (_, _) => OnPropertyChanged(nameof(AudioPlayerTime));
        NoAudioPlayer.PositionChanged += (_, _) => OnPropertyChanged(nameof(NoAudioPlayerPos));
        NoAudioPlayer.TimeChanged += (_, _) => OnPropertyChanged(nameof(NoAudioPlayerTime));
    }

    private void AudioPlayerOnEndReached(object? sender, EventArgs e)
    {
        if (CurrentPlayingGroup is null)
            return; //idk but yeah
        if (!IsPlaying)
            return; //stopped?

        if (CurrentPlayingGroup.Group.VideoGroup.WaitForBothVideosToFinish && NoAudioPlayer.State != VLCState.Ended)
            return;

        var indexOfCurrent = VideoGroups.IndexOf(CurrentPlayingGroup.Group);
        if (indexOfCurrent == VideoGroups.Count - 1)
        {
            _ = Dispatcher.UIThread.InvokeAsync(Stop);
            return;
        }

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
            await PlayFromBlock(VideoGroups[indexOfCurrent + 1]).ConfigureAwait(true));
    }

    private void NonAudioPlayerOnEndReached(object? sender, EventArgs e)
    {
        if (CurrentPlayingGroup is null)
            return; //idk but yeah
        if (!IsPlaying)
            return; //stopped?

        if (CurrentPlayingGroup.Group.VideoGroup.WaitForBothVideosToFinish && AudioPlayer.State != VLCState.Ended)
            return;

        var indexOfCurrent = VideoGroups.IndexOf(CurrentPlayingGroup.Group);
        if (indexOfCurrent == VideoGroups.Count - 1)
        {
            _ = Dispatcher.UIThread.InvokeAsync(Stop);
            return;
        }

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
            await PlayFromBlock(VideoGroups[indexOfCurrent + 1]).ConfigureAwait(true));
    }

    #region Playback Commands

    [RelayCommand]
    private void Stop()
    {
        IsPlaying = false;
        if (CurrentPlayingGroup is null)
            return;
        CurrentPlayingGroup.Group.IsPlaying = false;
        CurrentPlayingGroup?.Dispose();
        CurrentPlayingGroup = null;
        OnPropertyChanged(nameof(AudioPlayerPos));
        OnPropertyChanged(nameof(AudioPlayerTime));
        OnPropertyChanged(nameof(NoAudioPlayerPos));
        OnPropertyChanged(nameof(NoAudioPlayerTime));
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

        switch (group.VideoGroup.NonAudioOnMainScreen)
        {
            case true when !arePlayersSwitched:
                (AudioPlayer, NoAudioPlayer) = (NoAudioPlayer, AudioPlayer);
                arePlayersSwitched = true;
                break;
            case false when arePlayersSwitched:
                (AudioPlayer, NoAudioPlayer) = (NoAudioPlayer, AudioPlayer);
                arePlayersSwitched = false;
                break;
        }

        var pqi = new PlayQueueItem(group, AudioPlayer, NoAudioPlayer, _audioLibVlc, _noAudioLibVlc);
        await pqi.Initialize();

        IsPlaying = true;
        group.IsPlaying = true;
        CurrentPlayingGroup = pqi;

        pqi.Play();
    }

    [RelayCommand]
    private async Task PlayFromButton()
    {
        //Pause/Resume
        if (AudioPlayer.State == VLCState.Playing || NoAudioPlayer.State == VLCState.Playing)
        {
            CurrentPlayingGroup?.Pause();
        }
        else if (AudioPlayer.State == VLCState.Paused || NoAudioPlayer.State == VLCState.Paused)
        {
            CurrentPlayingGroup?.Resume();
        }
        //Play first group
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
        var index = CurrentPlayingGroup is null ? 0 : VideoGroups.IndexOf(CurrentPlayingGroup.Group);
        if (index == 0)
        {
            NotificationManager.Show(new Notification(
                "Cannot rewind",
                "You are already at the beginning of the playlist",
                NotificationType.Warning));
            return;
        }

        await PlayFromBlock(VideoGroups[index - 1]).ConfigureAwait(true);
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

        var index = CurrentPlayingGroup is null ? 0 : VideoGroups.IndexOf(CurrentPlayingGroup.Group);
        if (index == VideoGroups.Count - 1)
        {
            NotificationManager.Show(new Notification(
                "Cannot fast forward",
                "You are already at the end of the playlist",
                NotificationType.Warning));
            return;
        }

        await PlayFromBlock(VideoGroups[index + 1]).ConfigureAwait(true);
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
        await reorderWindow.ShowDialog(parent).ConfigureAwait(true);
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
        var result = await editWindow.ShowDialog<GroupWrapper?>(parent).ConfigureAwait(true);
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
            DefaultExtension = "mvc2",
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("MultiVideo Configuration")
                {
                    MimeTypes = new[] { "application/json" },
                    Patterns = new[] { "*.mvc2" }
                }
            }
        };

        var savePath = await parent.StorageProvider.SaveFilePickerAsync(sfo).ConfigureAwait(true);
        if (savePath is null)
            return;

        var savableGroups = VideoGroups.Select(x => SavableVideoGroup.FromVideoGroup(x.VideoGroup));
        var json = JsonSerializer.Serialize(savableGroups, _saveCtx.IEnumerableSavableVideoGroup);
        var path = savePath.TryGetLocalPath();
        if (path is null)
            return;

        await File.WriteAllTextAsync(path, json).ConfigureAwait(true);
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
                    Patterns = new[] { "*.mvc2" }
                }
            }
        };

        var files = await parent.StorageProvider.OpenFilePickerAsync(ofo).ConfigureAwait(true);
        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (path is null)
            return;

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(true);
        var savableGroups = JsonSerializer.Deserialize(json, _saveCtx.SavableVideoGroupArray);
        if (savableGroups is null)
            return;

        var missingGroups = savableGroups.Where(x =>
            (!string.IsNullOrEmpty(x.AudioVideoPath) && !File.Exists(x.AudioVideoPath)) ||
            (!string.IsNullOrEmpty(x.NonAudioVideoPath) && !File.Exists(x.NonAudioVideoPath)));
        if (missingGroups.Any())
        {
            var mvm = new MissingVideosViewModel(savableGroups);
            var mvw = new MissingVideosWindow()
            {
                DataContext = mvm
            };
            var mvwResult = await mvw.ShowDialog<bool?>(parent).ConfigureAwait(true);
            if (mvwResult is null or false)
                return;
        }

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
    
    [RelayCommand]
    private async Task ImportV1Groups(Window parent)
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

        var files = await parent.StorageProvider.OpenFilePickerAsync(ofo).ConfigureAwait(true);
        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (path is null)
            return;

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(true);
        var savableGroups = JsonSerializer.Deserialize(json, _saveCtx.OldVideoGroupArray);
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

    [RelayCommand]
    private void Exit(Window parent)
    {
        var l = Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        l?.Shutdown();
    }

    #endregion

    public void Dispose()
    {
        Stop();
        _mainVideoWindow?.Close();
        _lyricVideoWindow?.Close();
        AudioPlayer.Dispose();
        NoAudioPlayer.Dispose();
        _audioLibVlc.Dispose();
        _noAudioLibVlc.Dispose();
    }
}