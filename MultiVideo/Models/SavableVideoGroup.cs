using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace MultiVideo.Models;

public class SavableVideoGroup
{
    public string Title { get; set; } = "Untitled";
    public string? MainVideoPath { get; set; }
    public string? SecondaryVideoPath { get; set; }
    public TimeSpan MainVideoStartDelay { get; set; }
    public TimeSpan SecondaryVideoStartDelay { get; set; }
    public string? ThumbnailBase64 { get; set; }
    
    public static SavableVideoGroup FromVideoGroup(VideoGroup videoGroup)
    {
        var vg = new SavableVideoGroup
        {
            Title = videoGroup.Title,
            MainVideoPath = videoGroup.MainVideoPath,
            SecondaryVideoPath = videoGroup.SecondaryVideoPath,
            MainVideoStartDelay = videoGroup.MainVideoStartDelay,
            SecondaryVideoStartDelay = videoGroup.SecondaryVideoStartDelay
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
            MainVideoPath,
            SecondaryVideoPath,
            Title,
            MainVideoStartDelay,
            SecondaryVideoStartDelay
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
public partial class SavableVideoGroupContext : JsonSerializerContext
{
}