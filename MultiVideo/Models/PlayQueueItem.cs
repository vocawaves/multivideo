using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;

namespace MultiVideo.Models;

public partial class PlayQueueItem : ObservableObject, IDisposable
{
    [ObservableProperty] private GroupWrapper _group;

    [ObservableProperty] private TimeSpan _actualEndTime;

    public TimeSpan CurrentPosition
    {
        get => _currentPosition;
    }

    public double CurrentPosPerc => (1.0 / ActualEndTime.TotalMilliseconds) * CurrentPosition.TotalMilliseconds;

    private TimeSpan _internalStartTime = TimeSpan.Zero;
    private bool _isPaused = false;
    private bool _notTicked = true;
    private bool _resumed = false;
    private readonly Timer _internalTimer;
    private readonly LibVLC _audioLibVlc;
    private readonly LibVLC _nonAudioLibVlc;
    private readonly MediaPlayer _audioPlayer;
    private readonly MediaPlayer _nonAudioPlayer;
    private TimeSpan _currentPosition = TimeSpan.Zero;

    public PlayQueueItem(GroupWrapper group, MediaPlayer audioPlayer, MediaPlayer nonAudioPlayer,
        LibVLC audioLibVlc, LibVLC nonAudioLibVlc)
    {
        Group = group;
        _audioPlayer = audioPlayer;
        _nonAudioPlayer = nonAudioPlayer;
        _audioLibVlc = audioLibVlc;
        _nonAudioLibVlc = nonAudioLibVlc;
        _internalTimer = new Timer(1);
        _internalTimer.Elapsed += TimerTicked;
    }

    public async Task Initialize()
    {
        var audioMedia = new Media(_audioLibVlc, Group.VideoGroup.AudioVideoPath ?? string.Empty);
        var nonAudioMedia = new Media(_nonAudioLibVlc, Group.VideoGroup.NonAudioVideoPath ?? string.Empty);
        await Task.WhenAll(audioMedia.Parse(MediaParseOptions.FetchLocal), nonAudioMedia.Parse(MediaParseOptions.FetchLocal));

        var audioLength = audioMedia.Duration + Group.VideoGroup.AudioVideoStartDelay.TotalMilliseconds;
        var nonAudioLength = nonAudioMedia.Duration + Group.VideoGroup.NonAudioVideoStartDelay.TotalMilliseconds;
        ActualEndTime = audioLength > nonAudioLength
            ? TimeSpan.FromMilliseconds(audioLength)
            : TimeSpan.FromMilliseconds(nonAudioLength);

        _audioPlayer.Media = audioMedia;
        _nonAudioPlayer.Media = nonAudioMedia;
        //Play, pause and set position to 0 to ensure that the video is ready to play
    }

    public void Play()
    {
        _internalTimer.Start();
    }

    private void TimerTicked(object? sender, EventArgs e)
    {
        if (_isPaused)
            return;

        if (_notTicked)
        {
            _internalStartTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp());
            _notTicked = false;
        }

        if (_resumed)
        {
            _internalStartTime = TimeSpan.FromTicks(Stopwatch.GetTimestamp()) - CurrentPosition;
            _resumed = false;
        }

        var elapsed = TimeSpan.FromTicks(Stopwatch.GetTimestamp()) - _internalStartTime;
        if (elapsed > ActualEndTime)
            return; //how?? idk
        Dispatcher.UIThread.Post(() =>
        {
            _currentPosition = elapsed;
            if (elapsed.Milliseconds % 10 != 0) 
                return;
            OnPropertyChanged(nameof(CurrentPosition));
            OnPropertyChanged(nameof(CurrentPosPerc));
        });
        if (elapsed >= Group.VideoGroup.AudioVideoStartDelay && _audioPlayer.State != VLCState.Playing)
        {
            //start main vid
            _audioPlayer.Play();
        }

        if (elapsed >= Group.VideoGroup.NonAudioVideoStartDelay && _nonAudioPlayer.State != VLCState.Playing)
        {
            //start secondary vid
            _nonAudioPlayer.Play();
        }
    }

    public void Pause()
    {
        _audioPlayer.Pause();
        _nonAudioPlayer.Pause();
        _isPaused = true;
        _internalTimer.Stop();
    }

    public void Resume()
    {
        if (CurrentPosition > Group.VideoGroup.AudioVideoStartDelay)
            _audioPlayer.Play();
        if (CurrentPosition > Group.VideoGroup.NonAudioVideoStartDelay)
            _nonAudioPlayer.Play();
        _isPaused = false;
        //add to internal start time to account for pause
        _resumed = true;
        _internalTimer.Start();
    }

    public void Dispose()
    {
        _internalTimer.Dispose();
        _audioPlayer.Stop();
        _nonAudioPlayer.Stop();
        _audioPlayer.Media?.Dispose();
        _nonAudioPlayer.Media?.Dispose();
    }
}