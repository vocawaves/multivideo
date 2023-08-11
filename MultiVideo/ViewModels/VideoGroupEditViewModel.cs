using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiVideo.Models;

namespace MultiVideo.ViewModels;

public partial class VideoGroupEditViewModel : ViewModelBase
{
    [ObservableProperty] private GroupWrapper _group = null!;

    [ObservableProperty] private string? _thumbnailPath;

    [RelayCommand]
    private void Save(Window parent)
    {
        var validPaths = CheckPaths();
        if (!validPaths)
            return;

        if (ThumbnailPath != null)
        {
            using var bmp = new Bitmap(ThumbnailPath);
            Group.VideoGroup.Thumbnail = bmp.CreateScaledBitmap(new PixelSize(200, 200));
        }

        parent.Close(Group);
    }

    private bool CheckPaths()
    {
        Group.VideoGroup.AudioVideoPath = Group.VideoGroup.AudioVideoPath?.Trim().Replace("\"", "")!;
        Group.VideoGroup.NonAudioVideoPath = Group.VideoGroup.NonAudioVideoPath?.Trim().Replace("\"", "")!;
        ThumbnailPath = ThumbnailPath?.Trim().Replace("\"", "")!;

        return (File.Exists(Group.VideoGroup.AudioVideoPath) || File.Exists(Group.VideoGroup.NonAudioVideoPath)) 
            && (ThumbnailPath == null || File.Exists(ThumbnailPath));
    }

    [RelayCommand]
    private void Cancel(Window parent)
    {
        parent.Close();
    }
}