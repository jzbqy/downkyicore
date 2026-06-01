using Newtonsoft.Json;

namespace DownKyi.Core.Settings.Models;

public class DrmSettings
{
    [JsonProperty("isDrmEnabled")] public bool IsDrmEnabled { get; set; } = false;
    
    [JsonProperty("wvdFilePath")] public string? WvdFilePath { get; set; }
    
    [JsonProperty("mp4DecryptPath")] public string? Mp4DecryptPath { get; set; }
    
    [JsonProperty("autoKeyAcquisition")] public bool AutoKeyAcquisition { get; set; } = true;
    
    [JsonProperty("maxRetryCount")] public int MaxRetryCount { get; set; } = 3;
    
    [JsonProperty("retryDelaySeconds")] public int RetryDelaySeconds { get; set; } = 300;
    
    [JsonProperty("riskPauseMinutes")] public int RiskPauseMinutes { get; set; } = 10;
    
    [JsonProperty("continuousFailureThreshold")] public int ContinuousFailureThreshold { get; set; } = 3;
    
    [JsonProperty("autoCheckService")] public bool AutoCheckService { get; set; } = true;
}