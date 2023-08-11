using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace MultiVideo.Models;

public class SavableVideoGroup
{
    public string Title { get; set; } = "Untitled";
    public string? AudioVideoPath { get; set; }
    public string? NonAudioVideoPath { get; set; }
    public TimeSpan AudioVideoStartDelay { get; set; }
    public TimeSpan NonAudioVideoStartDelay { get; set; }
    public bool NonAudioOnMainScreen { get; set; } = false;
    public bool WaitForBothVideosToFinish { get; set; } = false;
    public string? ThumbnailBase64 { get; set; }
    
    public static SavableVideoGroup FromVideoGroup(VideoGroup videoGroup)
    {
        var vg = new SavableVideoGroup
        {
            Title = videoGroup.Title,
            AudioVideoPath = videoGroup.AudioVideoPath,
            NonAudioVideoPath = videoGroup.NonAudioVideoPath,
            AudioVideoStartDelay = videoGroup.AudioVideoStartDelay,
            NonAudioVideoStartDelay = videoGroup.NonAudioVideoStartDelay,
            NonAudioOnMainScreen = videoGroup.NonAudioOnMainScreen,
            WaitForBothVideosToFinish = videoGroup.WaitForBothVideosToFinish
        };
        if (videoGroup.Thumbnail is null)
            return vg;

        using var ms = new MemoryStream();
        videoGroup.Thumbnail?.Save(ms);
        vg.ThumbnailBase64 = Convert.ToBase64String(ms.GetBuffer());
        return vg;
    }
    
    public VideoGroup ToVideoGroup()
    {
        var vg = new VideoGroup(
            AudioVideoPath,
            NonAudioVideoPath,
            Title,
            AudioVideoStartDelay,
            NonAudioVideoStartDelay,
            NonAudioOnMainScreen,
            WaitForBothVideosToFinish
        );
        if (string.IsNullOrEmpty(ThumbnailBase64))
            return vg;

        var bytes = Convert.FromBase64String(ThumbnailBase64);
        using var ms = new MemoryStream(bytes);
        vg.Thumbnail = new(ms);
        return vg;
    }
}

[JsonSerializable(typeof(SavableVideoGroup))]
[JsonSerializable(typeof(SavableVideoGroup[]))]
[JsonSerializable(typeof(IEnumerable<SavableVideoGroup>))]
[JsonSerializable(typeof(OldVideoGroup))]
[JsonSerializable(typeof(OldVideoGroup[]))]
[JsonSerializable(typeof(IEnumerable<OldVideoGroup>))]
public partial class SavableVideoGroupContext : JsonSerializerContext
{
}