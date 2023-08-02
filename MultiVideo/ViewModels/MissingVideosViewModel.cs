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
            if (!string.IsNullOrEmpty(group.MainVideoPath) || !File.Exists(group.MainVideoPath))
            {
                var fileName = Path.GetFileName(group.MainVideoPath);
                var files = dirInfo.GetFiles(fileName!, SearchOption.AllDirectories);
                if (files.Length != 0)
                    group.MainVideoPath = files[0].FullName;
            }

            if (!string.IsNullOrEmpty(group.SecondaryVideoPath) || !File.Exists(group.SecondaryVideoPath))
            {
                var fileName = Path.GetFileName(group.SecondaryVideoPath);
                var files = dirInfo.GetFiles(fileName!, SearchOption.AllDirectories);
                if (files.Length != 0)
                    group.SecondaryVideoPath = files[0].FullName;
            }

            if ((!string.IsNullOrEmpty(group.MainVideoPath) && !File.Exists(group.MainVideoPath)) ||
                (!string.IsNullOrEmpty(group.SecondaryVideoPath) && !File.Exists(group.SecondaryVideoPath)))
                continue;

            MissingGroups.Remove(group);
            i--;
        }

        //if (MissingGroups.Count == 0)
        //    parent.Close(true);
    }
}