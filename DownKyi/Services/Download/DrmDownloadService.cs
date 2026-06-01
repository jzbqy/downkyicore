using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DownKyi.Core.BiliApi;
using DownKyi.Core.BiliApi.VideoStream.Models;
using DownKyi.Core.DRM;
using DownKyi.Core.Logging;
using DownKyi.Models;
using DownKyi.PrismExtension.Dialog;
using DownKyi.Utils;
using DownKyi.ViewModels;
using DownKyi.ViewModels.DownloadManager;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.Services.Download;

public class DrmDownloadService : DownloadService
{
    public DrmDownloadService(ImmutableObservableCollection<DownloadingItem> downloadingList, 
        ImmutableObservableCollection<DownloadedItem> downloadedList, 
        IDialogService? dialogService) 
        : base(downloadingList, downloadedList, dialogService)
    {
        Tag = "DrmDownloadService";
    }

    public override void Parse(DownloadingItem downloading)
    {
        base.BaseParse(downloading);
        
        if (downloading.PlayUrl?.Dash?.Video != null)
        {
            foreach (var video in downloading.PlayUrl.Dash.Video)
            {
                if (video.Codecs?.Contains("cenc") == true || video.Codecs?.Contains("dash") == true)
                {
                    downloading.Downloading.IsDrmEncrypted = true;
                    downloading.Downloading.QueueType = DownloadQueueType.Drm;
                    LogManager.Info(Tag, $"检测到DRM加密视频: {downloading.DownloadBase.Bvid}");
                    break;
                }
            }
        }
    }

    public override string DownloadAudio(DownloadingItem downloading)
    {
        var audio = BaseDownloadAudio(downloading);
        if (audio == null) return null;

        if (downloading.Downloading.IsDrmEncrypted)
        {
            return DownloadDrmAudio(downloading, audio);
        }

        return base.BaseDownloadAudio(downloading)?.BaseUrl ?? null;
    }

    public override string DownloadVideo(DownloadingItem downloading)
    {
        var video = BaseDownloadVideo(downloading);
        if (video == null) return null;

        if (downloading.Downloading.IsDrmEncrypted)
        {
            return DownloadDrmVideo(downloading, video);
        }

        return video.BaseUrl;
    }

    public override string DownloadDanmaku(DownloadingItem downloading)
    {
        return BaseDownloadDanmaku(downloading);
    }

    public override List<string> DownloadSubtitle(DownloadingItem downloading)
    {
        return BaseDownloadSubtitle(downloading);
    }

    public override string DownloadCover(DownloadingItem downloading, string coverUrl, string fileName)
    {
        return BaseDownloadCover(downloading, coverUrl, fileName);
    }

    public override string MixedFlow(DownloadingItem downloading, string? audioUid, string? videoUid)
    {
        if (downloading.Downloading.IsDrmEncrypted)
        {
            return DecryptAndMixedFlow(downloading, audioUid, videoUid);
        }
        
        return BaseMixedFlow(downloading, audioUid, videoUid);
    }

    protected override void Pause(DownloadingItem downloading)
    {
        downloading.DownloadStatusTitle = DictionaryResource.GetString("Pausing");
        downloading.DownloadContent = string.Empty;
    }

    public void Start()
    {
        BaseStart();
        DrmTaskManager.Start();
    }

    public void End()
    {
        BaseEndTask().Wait();
        DrmTaskManager.Stop();
    }

    private string? DownloadDrmAudio(DownloadingItem downloading, PlayUrlDashVideo audio)
    {
        try
        {
            var encryptedFile = $"{downloading.DownloadBase.FilePath}.audio.encrypted";
            WebClient.DownloadFile(audio.BaseUrl, encryptedFile);
            downloading.Downloading.DownloadFiles.TryAdd("audio_encrypted", encryptedFile);
            return encryptedFile;
        }
        catch (Exception ex)
        {
            LogManager.Error(Tag, $"DRM音频下载失败: {ex.Message}");
            return null;
        }
    }

    private string? DownloadDrmVideo(DownloadingItem downloading, VideoPlayUrlBasic video)
    {
        try
        {
            var encryptedFile = $"{downloading.DownloadBase.FilePath}.video.encrypted";
            WebClient.DownloadFile(video.BaseUrl, encryptedFile);
            downloading.Downloading.DownloadFiles.TryAdd("video_encrypted", encryptedFile);
            return encryptedFile;
        }
        catch (Exception ex)
        {
            LogManager.Error(Tag, $"DRM视频下载失败: {ex.Message}");
            return null;
        }
    }

    private string DecryptAndMixedFlow(DownloadingItem downloading, string? audioUid, string? videoUid)
    {
        downloading.DownloadStatusTitle = "DRM解密中";
        downloading.DownloadContent = "正在获取DRM密钥...";

        var pssh = ExtractPsshFromPlayUrl(downloading.PlayUrl);
        
        var taskInfo = new DrmTaskInfo
        {
            TaskId = Guid.NewGuid().ToString(),
            Cid = downloading.DownloadBase.Cid,
            Bvid = downloading.DownloadBase.Bvid,
            Avid = downloading.DownloadBase.Avid,
            VideoUrl = videoUid ?? string.Empty,
            AudioUrl = audioUid ?? string.Empty,
            OutputPath = downloading.DownloadBase.FilePath,
            OutputFileName = downloading.Name,
            Pssh = pssh
        };

        DrmTaskManager.AddTask(taskInfo);

        while (taskInfo.Status != DrmStatus.Completed && taskInfo.Status != DrmStatus.Failed)
        {
            System.Threading.Thread.Sleep(1000);
        }

        if (taskInfo.Status == DrmStatus.Completed)
        {
            return $"{downloading.DownloadBase.FilePath}.mp4";
        }
        
        downloading.DownloadStatusTitle = "DRM解密失败";
        downloading.DownloadContent = taskInfo.ErrorMessage;
        return null;
    }

    private string ExtractPsshFromPlayUrl(PlayUrl? playUrl)
    {
        if (playUrl?.Dash?.Video == null) return string.Empty;
        
        foreach (var video in playUrl.Dash.Video)
        {
            if (!string.IsNullOrEmpty(video.Pssh))
            {
                return video.Pssh;
            }
        }
        
        return string.Empty;
    }
}