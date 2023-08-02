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
    private readonly LibVLC _lyricAudioLibVlc;


    [ObservableProperty] private MediaPlayer _mainPlayer;

    [ObservableProperty] private MediaPlayer _lyricPlayer;

    [ObservableProperty] private MediaPlayer _lyricAudioPlayer;

    [ObservableProperty] private bool _isAudioLyricInUse = false;

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
        
        //string[] mainOptions = new string[]
        //{
        //    "--http-port=8080",
        //    "--http-password=kaito"
        //};
        
        string[] lyricOptions = new string[]
        {
          //  "--http-port=8081",
         //   "--http-password=kaito",
            "--no-audio"
        };

        _mainLibVlc = new LibVLC();
        _lyricLibVlc = new LibVLC(lyricOptions);
        _lyricAudioLibVlc = new LibVLC();
        
        //funky http for Numark
        //try
        //{
        //    _mainLibVlc.AddInterface("http");
        //    _lyricLibVlc.AddInterface("http");
        //}
        //catch
        //{
            // ignored
        //}
        
        //Init MediaPlayers
        MainPlayer = new MediaPlayer(_mainLibVlc);
        LyricPlayer = new MediaPlayer(_lyricLibVlc);
        LyricAudioPlayer = new MediaPlayer(_lyricAudioLibVlc);
        //LyricPlayer.SetAudioOutput("adummy"); //basically mute

        MainPlayer.EndReached += MainPlayerOnEndReached;
        MainPlayer.PositionChanged += MainPlayerOnPositionChanged;
        LyricAudioPlayer.PositionChanged += MainPlayerOnPositionChanged;
    }

    partial void OnMainRealPositionChanged(float oldValue, float newValue)
    {
        var oldPosInTime = TimeSpan.FromMilliseconds(oldValue * (IsAudioLyricInUse ? LyricAudioPlayer.Length : MainPlayer.Length));
        var newPosInTime = TimeSpan.FromMilliseconds(newValue * (IsAudioLyricInUse ? LyricAudioPlayer.Length : MainPlayer.Length));
        var diff = newPosInTime - oldPosInTime;
        if (!(diff.TotalSeconds > 1) && !(diff.TotalSeconds < 0)) 
            return;
        MainPlayer.PositionChanged -= MainPlayerOnPositionChanged;
        LyricAudioPlayer.PositionChanged -= MainPlayerOnPositionChanged;
        MainPlayer.Position = newValue;
        if (IsAudioLyricInUse)
            LyricAudioPlayer.Position = newValue;
        else
            LyricPlayer.Position = newValue;
        MainPlayer.PositionChanged += MainPlayerOnPositionChanged;
        LyricAudioPlayer.PositionChanged += MainPlayerOnPositionChanged;
    }

    private void MainPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        MainPosition = TimeSpan.FromMilliseconds(e.Position * (IsAudioLyricInUse ? LyricAudioPlayer.Length : MainPlayer.Length));
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
            _ = Dispatcher.UIThread.InvokeAsync(Stop);
            return;
        }

        _ = Dispatcher.UIThread.InvokeAsync(async () => await PlayFromBlock(VideoGroups[indexOfCurrent + 1]).ConfigureAwait(true));
    }

    partial void OnVolumeChanged(int value)
    {
        MainPlayer.Volume = value;
        LyricAudioPlayer.Volume = value;
    }

    #region Playback Commands

    [RelayCommand]
    private void Stop()
    {
        IsPlaying = false;
        MainPlayer.Stop();
        LyricPlayer.Stop();
        LyricAudioPlayer.Stop();
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

        IsAudioLyricInUse = string.IsNullOrEmpty(group.VideoGroup.MainVideoPath);

        var mainVlcToUse = _mainLibVlc;
        var lyricVlcToUse = IsAudioLyricInUse ? _lyricAudioLibVlc : _lyricLibVlc;
        var mainPlayerToUse = MainPlayer;
        var lyricPlayerToUse = IsAudioLyricInUse ? LyricAudioPlayer : LyricPlayer;

        MainMedia = !string.IsNullOrEmpty(group.VideoGroup.MainVideoPath)
            ? new Media(mainVlcToUse, group.VideoGroup.MainVideoPath)
            : null;
        if (MainMedia is not null)
            await MainMedia.Parse().ConfigureAwait(true);
        LyricMedia = !string.IsNullOrEmpty(group.VideoGroup.SecondaryVideoPath)
            ? new Media(lyricVlcToUse, group.VideoGroup.SecondaryVideoPath)
            : null;
        if (LyricMedia is not null)
            await LyricMedia.Parse().ConfigureAwait(true);

        mainPlayerToUse.Media = MainMedia;
        lyricPlayerToUse.Media = LyricMedia;
        

        var waitForMain = group.VideoGroup.MainVideoStartDelay > group.VideoGroup.SecondaryVideoStartDelay;
        var actualWaitTime = waitForMain
            ? (group.VideoGroup.MainVideoStartDelay - group.VideoGroup.SecondaryVideoStartDelay)
            : (group.VideoGroup.SecondaryVideoStartDelay - group.VideoGroup.MainVideoStartDelay);

        //High speed case
        if (group.VideoGroup.MainVideoStartDelay == TimeSpan.Zero &&
            group.VideoGroup.SecondaryVideoStartDelay == TimeSpan.Zero)
        {
            mainPlayerToUse.Play();
            lyricPlayerToUse.Play();
        }
        else if (waitForMain)
        {
            lyricPlayerToUse.Play();
            await Task.Delay(actualWaitTime).ConfigureAwait(true);
            mainPlayerToUse.Play();
        }
        else
        {
            mainPlayerToUse.Play();
            await Task.Delay(actualWaitTime).ConfigureAwait(true);
            lyricPlayerToUse.Play();
        }

        IsPlaying = true;
        group.IsPlaying = true;
        CurrentPlayingGroup = group;
    }

    [RelayCommand]
    private async Task PlayFromButton()
    {
        //Pause
        if (MainPlayer.State == VLCState.Playing || LyricPlayer.State == VLCState.Playing || LyricAudioPlayer.State == VLCState.Playing)
        {
            MainPlayer.Pause();
            if (IsAudioLyricInUse)
                LyricAudioPlayer.Pause();
            else
                LyricPlayer.Pause();
        }
        else if (MainPlayer.State == VLCState.Paused || LyricPlayer.State == VLCState.Paused || LyricAudioPlayer.State == VLCState.Paused)
        {
            MainPlayer.Play();
            if (IsAudioLyricInUse)
                LyricAudioPlayer.Play();
            else
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
        
        var index = CurrentPlayingGroup is null ? 0 : VideoGroups.IndexOf(CurrentPlayingGroup);
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
        var savableGroups = JsonSerializer.Deserialize(json, _saveCtx.SavableVideoGroupArray);
        if (savableGroups is null)
            return;

        var missingGroups = savableGroups.Where(x =>
            (!string.IsNullOrEmpty(x.MainVideoPath) && !File.Exists(x.MainVideoPath)) ||
            (!string.IsNullOrEmpty(x.SecondaryVideoPath) && !File.Exists(x.SecondaryVideoPath)));
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
        MainPlayer.Dispose();
        LyricPlayer.Dispose();
        LyricAudioPlayer.Dispose();
        _mainLibVlc.Dispose();
        _lyricLibVlc.Dispose();
        _lyricAudioLibVlc.Dispose();
    }
}