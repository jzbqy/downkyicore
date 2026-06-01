namespace DownKyi.Models;

public enum DownloadQueueType
{
    Normal = 0,
    Drm = 1
}

public enum DrmStatus
{
    None = 0,
    Pending = 1,
    AcquiringKey = 2,
    Downloading = 3,
    Decrypting = 4,
    Completed = 5,
    Failed = 6,
    RiskPaused = 7
}

public enum DrmErrorType
{
    None = 0,
    NetworkError = 1,
    KeyAcquisitionFailed = 2,
    DecryptionFailed = 3,
    FileNotFound = 4,
    InvalidWvd = 5,
    EnvironmentError = 6,
    RiskControl = 7
}

public enum WvdSecurityLevel
{
    Unknown = 0,
    L1 = 1,
    L3 = 3
}