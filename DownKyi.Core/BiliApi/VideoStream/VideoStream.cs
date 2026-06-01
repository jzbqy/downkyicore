using System.Text.RegularExpressions;
using System.Web;
using DownKyi.Core.BiliApi.Login;
using DownKyi.Core.BiliApi.Models.Json;
using DownKyi.Core.BiliApi.Sign;
using DownKyi.Core.BiliApi.VideoStream.Models;
using DownKyi.Core.Logging;
using Newtonsoft.Json;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.Core.BiliApi.VideoStream;

public static class VideoStream
{
    /// <summary>
    /// 获取播放器信息（web端）
    /// </summary>
    /// <param name="avid"></param>
    /// <param name="bvid"></param>
    /// <param name="cid"></param>
    /// <returns></returns>
    public static PlayerV2? PlayerV2(long avid, string? bvid, long cid)
    {
        var parameters = new Dictionary<string, object?>();

        if (avid > 0)
        {
            parameters.Add("bvid", bvid);
        }

        if (bvid != null)
        {
            parameters.Add("avid", avid);
        }

        if (cid > 0)
        {
            parameters.Add("cid", cid);
        }

        var query = WbiSign.ParametersToQuery(WbiSign.EncodeWbi(parameters));
        var url = $"https://api.bilibili.com/x/player/wbi/v2?{query}";
        const string referer = "https://www.bilibili.com";
        var response = WebClient.RequestWeb(url, referer);

        try
        {
            var playUrl = JsonConvert.DeserializeObject<PlayerV2Origin>(response);
            return playUrl?.Data;
        }
        catch (Exception e)
        {
            Console.PrintLine("PlayerV2()发生异常: {0}", e);
            LogManager.Error("PlayerV2()", e);
            return null;
        }
    }

    /// <summary>
    /// 获取所有字幕<br/>
    /// 若视频没有字幕，返回null
    /// </summary>
    /// <param name="avid"></param>
    /// <param name="bvid"></param>
    /// <param name="cid"></param>
    /// <returns></returns>
    public static List<SubRipText>? GetSubtitle(long avid, string? bvid, long cid)
    {
        var subRipTexts = new List<SubRipText>();

        // 获取播放器信息
        var player = PlayerV2(avid, bvid, cid);
        if (player == null)
        {
            return subRipTexts;
        }

        if (player.Subtitle != null && player.Subtitle.Subtitles != null && player.Subtitle.Subtitles.Count == 0)
        {
            return null;
        }

        foreach (var subtitle in player.Subtitle.Subtitles)
        {
            const string referer = "https://www.bilibili.com";
            var response = WebClient.RequestWeb($"https:{subtitle.SubtitleUrl}", referer);

            try
            {
                var subtitleJson = JsonConvert.DeserializeObject<SubtitleJson>(response);
                if (subtitleJson == null)
                {
                    continue;
                }

                subRipTexts.Add(new SubRipText
                {
                    Lan = subtitle.Lan,
                    LanDoc = subtitle.LanDoc,
                    SrtString = subtitleJson.ToSubRip()
                });
            }
            catch (Exception e)
            {
                Console.PrintLine("GetSubtitle()发生异常: {0}", e);
                LogManager.Error("GetSubtitle()", e);
            }
        }

        return subRipTexts;
    }

    /// <summary>
    /// 获取普通视频的视频流
    /// </summary>
    /// <param name="avid"></param>
    /// <param name="bvid"></param>
    /// <param name="cid"></param>
    /// <param name="quality"></param>
    /// <returns></returns>
    public static PlayUrl? GetVideoPlayUrl(long avid, string bvid, long cid, int quality = 125)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "fourk", 1 },
            { "fnver", 0 },
            { "fnval", 4048 },
            { "cid", cid },
            { "qn", quality },
        };

        if (bvid != null)
        {
            parameters.Add("bvid", bvid);
        }
        else if (avid > -1)
        {
            parameters.Add("aid", avid);
        }
        else
        {
            return null;
        }

        var query = WbiSign.ParametersToQuery(WbiSign.EncodeWbi(parameters));
        var url = $"https://api.bilibili.com/x/player/wbi/playurl?{query}";

        return GetPlayUrl(url);
    }

    /// <summary>
    /// 获取普通视频的视频流（WebPage方式）
    /// </summary>
    /// <param name="avid"></param>
    /// <param name="bvid"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static PlayUrl? GetVideoPlayUrlWebPage(long avid, string bvid, long cid, int p)
    {
        var url = "https://www.bilibili.com/video";
        if (bvid == string.Empty)
        {
            url = $"{url}/{bvid}/?p={p}";
        }
        else if (avid > -1)
        {
            url = $"{url}/av{avid}/?p={p}";
        }

        var playUrl = GetPlayUrlWebPage(url);
        if (playUrl == null)
        {
            playUrl = GetVideoPlayUrl(avid, bvid, cid);
        }

        return playUrl;
    }

    /// <summary>
/// 获取番剧的视频流
/// </summary>
/// <param name="avid"></param>
/// <param name="bvid"></param>
/// <param name="cid"></param>
/// <param name="quality"></param>
/// <returns></returns>
public static PlayUrl? GetBangumiPlayUrl(long avid, string bvid, long cid, int quality = 125)
{
    return GetBangumiPlayUrlWithDrm(avid, bvid, cid, quality);
}

/// <summary>
/// 获取番剧的视频流（支持DRM高码率）
/// 使用新的 /ogv/player/playview 接口，POST请求
/// </summary>
/// <param name="avid"></param>
/// <param name="bvid"></param>
/// <param name="cid"></param>
/// <param name="quality"></param>
/// <returns></returns>
public static PlayUrl? GetBangumiPlayUrlWithDrm(long avid, string bvid, long cid, int quality = 125)
{
    var parameters = new Dictionary<string, object?>
    {
        { "cid", cid },
        { "qn", quality },
        { "fourk", 1 },
        { "fnver", 0 },
        { "fnval", 4048 },
        { "drm_tech_type", 2 },
        { "ep_id", 0 }
    };

    if (!string.IsNullOrEmpty(bvid))
    {
        parameters.Add("bvid", bvid);
    }
    else if (avid > -1)
    {
        parameters.Add("aid", avid);
    }
    else
    {
        return null;
    }

    var wbiParams = WbiSign.EncodeWbi(parameters);
    var query = WbiSign.ParametersToQuery(wbiParams);
    var url = $"https://api.bilibili.com/ogv/player/playview?{query}";

    return GetPlayUrlWithPost(url);
}

/// <summary>
/// 使用POST方式获取视频流
/// </summary>
/// <param name="url"></param>
/// <returns></returns>
private static PlayUrl? GetPlayUrlWithPost(string url)
{
    var csrfToken = GetCsrfToken();
    const string referer = "https://www.bilibili.com";
    
    var requestBody = new Dictionary<string, object?>
    {
        { "cid", ExtractParamFromUrl(url, "cid") },
        { "qn", ExtractParamFromUrl(url, "qn") },
        { "fourk", 1 },
        { "fnver", 0 },
        { "fnval", 4048 },
        { "drm_tech_type", 2 },
        { "ep_id", 0 }
    };

    var bvid = ExtractParamFromUrl(url, "bvid");
    if (!string.IsNullOrEmpty(bvid))
    {
        requestBody.Add("bvid", bvid);
    }
    var aid = ExtractParamFromUrl(url, "aid");
    if (!string.IsNullOrEmpty(aid))
    {
        requestBody.Add("aid", long.Parse(aid));
    }

    try
    {
        var response = WebClient.RequestWeb(url, referer, "POST", requestBody, json: true);
        
        var playUrl = JsonConvert.DeserializeObject<PlayUrlOrigin>(response);
        if (playUrl == null)
        {
            return null;
        }

        if (playUrl.Data != null)
        {
            return playUrl.Data;
        }

        if (playUrl.Result != null)
        {
            return playUrl.Result;
        }
        else
        {
            return null;
        }
    }
    catch (Exception e)
    {
        Console.PrintLine("GetPlayUrlWithPost()发生异常: {0}", e);
        LogManager.Error("GetPlayUrlWithPost()", e);
        return null;
    }
}

/// <summary>
/// 从URL中提取参数
/// </summary>
/// <param name="url"></param>
/// <param name="paramName"></param>
/// <returns></returns>
private static string? ExtractParamFromUrl(string url, string paramName)
{
    try
    {
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query[paramName];
    }
    catch
    {
        return null;
    }
}

/// <summary>
/// 获取CSRF Token（bili_jct）
/// </summary>
/// <returns></returns>
private static string? GetCsrfToken()
{
    try
    {
        var cookies = LoginHelper.GetLoginInfoCookies();
        var csrfCookie = cookies.FirstOrDefault(c => c.Name == "bili_jct");
        return csrfCookie?.Value;
    }
    catch
    {
        return null;
    }
}

    /// <summary>
    /// 获取课程的视频流
    /// </summary>
    /// <param name="avid"></param>
    /// <param name="bvid"></param>
    /// <param name="cid"></param>
    /// <param name="quality"></param>
    /// <returns></returns>
    public static PlayUrl GetCheesePlayUrl(long avid, string bvid, long cid, long episodeId, int quality = 125)
    {
        var baseUrl = $"https://api.bilibili.com/pugv/player/web/playurl?cid={cid}&qn={quality}&fourk=1&fnver=0&fnval=4048";
        string url;
        if (bvid != null)
        {
            url = $"{baseUrl}&bvid={bvid}";
        }
        else if (avid > -1)
        {
            url = $"{baseUrl}&aid={avid}";
        }
        else
        {
            return null;
        }

        // 必须有episodeId，否则会返回请求错误
        if (episodeId != 0)
        {
            url += $"&ep_id={episodeId}";
        }

        return GetPlayUrl(url);
    }

    /// <summary>
    /// 获取视频流
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static PlayUrl? GetPlayUrl(string url)
    {
        const string referer = "https://www.bilibili.com";
        var response = WebClient.RequestWeb(url, referer);

        try
        {
            var playUrl = JsonConvert.DeserializeObject<PlayUrlOrigin>(response);
            if (playUrl == null)
            {
                return null;
            }

            if (playUrl.Data != null)
            {
                return playUrl.Data;
            }

            if (playUrl.Result != null)
            {
                return playUrl.Result;
            }
            else
            {
                return null;
            }
        }
        catch (Exception e)
        {
            Console.PrintLine("GetPlayUrl()发生异常: {0}", e);
            LogManager.Error("GetPlayUrl()", e);
            return null;
        }
    }

    /// <summary>
    /// 获取视频流（WebPage方式）
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static PlayUrl? GetPlayUrlWebPage(string url)
    {
        const string referer = "https://www.bilibili.com";
        var response = WebClient.RequestWeb(url, referer);

        try
        {
            var regex = new Regex(@"<script>window\.__playinfo__=(.*?)<\/script>");
            var m = regex.Match(response);
            PlayUrlOrigin? playUrl = null;
            if (m.Success)
            {
                playUrl = JsonConvert.DeserializeObject<PlayUrlOrigin>(m.Groups[1].ToString());
            }

            if (playUrl == null)
            {
                return null;
            }

            if (playUrl.Data != null)
            {
                return playUrl.Data;
            }

            if (playUrl.Result != null)
            {
                return playUrl.Result;
            }

            return null;
        }
        catch (Exception e)
        {
            Console.PrintLine("GetPlayUrlPc()发生异常: {0}", e);
            LogManager.Error("GetPlayUrlPc()", e);
            return null;
        }
    }
}