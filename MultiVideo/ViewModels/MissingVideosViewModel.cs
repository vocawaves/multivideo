using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiVideo.Models;

namespace MultiVideo.ViewModels;

public partial class MissingVideosViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<SavableVideoGroup> _missingGroups;

    public MissingVideosViewModel(IEnumerable<SavableVideoGroup>? missing = null)
    {
        MissingGroups = missing is null 
            ? new() //should never happen, maybe in design
            : new(missing); 
    }

    [RelayCommand]
    private void Continue(Window parent)
    {
        parent.Close(true);
    }

    [RelayCommand]
    private void Cancel(Window parent)
    {
        parent.Close(false);
    }

    [RelayCommand]
    private async Task SelectDirectory(Window parent)
    {
        var dirPickerOptions = new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Select the folder the missing videos are located in"
        };
        var result = await parent.StorageProvider.OpenFolderPickerAsync(dirPickerOptions);
        if (result.Count == 0)
            return;

        var path = result[0].TryGetLocalPath();
        if (path is null || !Directory.Exists(path))
            return;

        var dirInfo = new DirectoryInfo(path);
        for (var i = 0; i < MissingGroups.Count; i++)
        {
            var group = MissingGroups[i];
            if (!string.IsNullOrEmpty(group.AudioVideoPath) && !File.Exists(group.NonAudioVideoPath))
            {
                var fileName = Path.GetFileName(group.AudioVideoPath);
                var files = dirInfo.GetFiles(fileName!, SearchOption.AllDirectories);
                if (files.Length != 0)
                    group.AudioVideoPath = files[0].FullName;
            }

            if (!string.IsNullOrEmpty(group.NonAudioVideoPath) && !File.Exists(group.NonAudioVideoPath))
            {
                var fileName = Path.GetFileName(group.NonAudioVideoPath);
                var files = dirInfo.GetFiles(fileName!, SearchOption.AllDirectories);
                if (files.Length != 0)
                    group.NonAudioVideoPath = files[0].FullName;
            }

            if ((!string.IsNullOrEmpty(group.AudioVideoPath) && !File.Exists(group.AudioVideoPath)) ||
                (!string.IsNullOrEmpty(group.NonAudioVideoPath) && !File.Exists(group.NonAudioVideoPath)))
                continue;

            MissingGroups.Remove(group);
            i--;
        }

        //if (MissingGroups.Count == 0)
        //    parent.Close(true);
    }
}