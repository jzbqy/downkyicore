using Newtonsoft.Json;

namespace DownKyi.Models;

public class DrmTaskInfo
{
    [JsonProperty("taskId")] public string TaskId { get; set; } = string.Empty;
    
    [JsonProperty("cid")] public long Cid { get; set; }
    
    [JsonProperty("bvid")] public string Bvid { get; set; } = string.Empty;
    
    [JsonProperty("avid")] public long Avid { get; set; }
    
    [JsonProperty("videoUrl")] public string VideoUrl { get; set; } = string.Empty;
    
    [JsonProperty("audioUrl")] public string AudioUrl { get; set; } = string.Empty;
    
    [JsonProperty("outputPath")] public string OutputPath { get; set; } = string.Empty;
    
    [JsonProperty("outputFileName")] public string OutputFileName { get; set; } = string.Empty;
    
    [JsonProperty("pssh")] public string Pssh { get; set; } = string.Empty;
    
    [JsonProperty("keys")] public List<DrmKeyInfo> Keys { get; set; } = new();
    
    [JsonProperty("status")] public DrmStatus Status { get; set; } = DrmStatus.Pending;
    
    [JsonProperty("errorType")] public DrmErrorType ErrorType { get; set; } = DrmErrorType.None;
    
    [JsonProperty("errorMessage")] public string ErrorMessage { get; set; } = string.Empty;
    
    [JsonProperty("retryCount")] public int RetryCount { get; set; } = 0;
    
    [JsonProperty("createTime")] public DateTime CreateTime { get; set; } = DateTime.Now;
    
    [JsonProperty("startTime")] public DateTime? StartTime { get; set; }
    
    [JsonProperty("completeTime")] public DateTime? CompleteTime { get; set; }
}

public class DrmKeyInfo
{
    [JsonProperty("kid")] public string Kid { get; set; } = string.Empty;
    
    [JsonProperty("key")] public string Key { get; set; } = string.Empty;
    
    [JsonProperty("type")] public string Type { get; set; } = "cenc";
}