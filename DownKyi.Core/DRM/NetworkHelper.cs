using System.Net.Http;
using DownKyi.Core.Logging;

namespace DownKyi.Core.DRM;

public static class NetworkHelper
{
    private static readonly HttpClient _httpClient = new();

    static NetworkHelper()
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DownKyi/1.0");
    }

    public static bool IsNetworkAvailable()
    {
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = ping.Send("www.bilibili.com", 3000);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsBilibiliAccessible()
    {
        try
        {
            var response = _httpClient.GetAsync("https://api.bilibili.com/x/web-interface/nav").Result;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsDrmServiceAvailable()
    {
        return IsNetworkAvailable() && IsBilibiliAccessible();
    }

    public static async Task<bool> IsNetworkAvailableAsync()
    {
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync("www.bilibili.com", 3000);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsBilibiliAccessibleAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.bilibili.com/x/web-interface/nav");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsDrmServiceAvailableAsync()
    {
        return await IsNetworkAvailableAsync() && await IsBilibiliAccessibleAsync();
    }
}