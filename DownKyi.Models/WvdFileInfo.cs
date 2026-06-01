using Newtonsoft.Json;

namespace DownKyi.Models;

public class WvdFileInfo
{
    [JsonProperty("fileName")] public string FileName { get; set; } = string.Empty;
    
    [JsonProperty("filePath")] public string FilePath { get; set; } = string.Empty;
    
    [JsonProperty("deviceType")] public string DeviceType { get; set; } = string.Empty;
    
    [JsonProperty("securityLevel")] public WvdSecurityLevel SecurityLevel { get; set; } = WvdSecurityLevel.Unknown;
    
    [JsonProperty("isValid")] public bool IsValid { get; set; } = false;
    
    [JsonProperty("validationError")] public string ValidationError { get; set; } = string.Empty;
    
    [JsonProperty("deviceName")] public string DeviceName { get; set; } = string.Empty;
    
    [JsonProperty("systemId")] public string SystemId { get; set; } = string.Empty;
    
    [JsonProperty("createdAt")] public DateTime? CreatedAt { get; set; }
}

public class WvdValidationResult
{
    public bool IsValid { get; set; } = false;
    public string FileName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string SecurityLevel { get; set; } = string.Empty;
    public string ValidationError { get; set; } = string.Empty;
    public WvdFileInfo? FileInfo { get; set; }
}