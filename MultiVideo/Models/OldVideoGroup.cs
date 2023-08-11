using System;
using System.IO;

namespace MultiVideo.Models;

public class OldVideoGroup
{
    public string Title { get; set; } = "Untitled";
    public string? MainVideoPath { get; set; }
    public string? SecondaryVideoPath { get; set; }
    public TimeSpan MainVideoStartDelay { get; set; }
    public TimeSpan SecondaryVideoStartDelay { get; set; }
    public string? ThumbnailBase64 { get; set; }
    
    public VideoGroup ToVideoGroup()
    {
        var vg = new VideoGroup(
            MainVideoPath,
            SecondaryVideoPath,
            Title,
            MainVideoStartDelay,
            SecondaryVideoStartDelay,
            false, true
        );
        if (string.IsNullOrEmpty(ThumbnailBase64))
            return vg;

        var bytes = Convert.FromBase64String(ThumbnailBase64);
        using var ms = new MemoryStream(bytes);
        vg.Thumbnail = new(ms);
        return vg;
    }
}