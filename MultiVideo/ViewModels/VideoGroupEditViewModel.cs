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
            //Is 0 valid for auto scaling?
            Group.VideoGroup.Thumbnail = bmp.CreateScaledBitmap(new PixelSize(200, 200));
        }
        
        parent.Close(Group);
    }

    private bool CheckPaths()
    {
        Group.VideoGroup.MainVideoPath = Group.VideoGroup.MainVideoPath?.Trim().Replace("\"", "")!;
        Group.VideoGroup.SecondaryVideoPath = Group.VideoGroup.SecondaryVideoPath?.Trim().Replace("\"", "")!;
        ThumbnailPath = ThumbnailPath?.Trim().Replace("\"", "")!;
        
        return File.Exists(Group.VideoGroup.MainVideoPath) &&
               File.Exists(Group.VideoGroup.SecondaryVideoPath) &&
               (ThumbnailPath == null || File.Exists(ThumbnailPath));
    }

    [RelayCommand]
    private void Cancel(Window parent)
    {
        parent.Close();
    }
}