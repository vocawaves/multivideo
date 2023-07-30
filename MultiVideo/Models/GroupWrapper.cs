using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiVideo.Models;

public partial class GroupWrapper : ObservableObject
{
    [ObservableProperty] private bool _isInitial = true;
    
    [ObservableProperty] private bool _isPlaying = false;
    
    [ObservableProperty] private VideoGroup _videoGroup = null!;

}