using DownKyi.Core.Settings.Models;

namespace DownKyi.Core.Settings;

public partial class SettingsManager
{
    private DrmSettings DrmSettings => _appSettings.DrmSettings ??= new DrmSettings();

    public bool GetDrmEnabled()
    {
        return DrmSettings.IsDrmEnabled;
    }

    public bool SetDrmEnabled(bool value)
    {
        return SetProperty(DrmSettings.IsDrmEnabled, value, v => DrmSettings.IsDrmEnabled = v);
    }

    public string? GetWvdFilePath()
    {
        return DrmSettings.WvdFilePath;
    }

    public bool SetWvdFilePath(string? value)
    {
        return SetProperty(DrmSettings.WvdFilePath, value, v => DrmSettings.WvdFilePath = v);
    }

    public string? GetMp4DecryptPath()
    {
        return DrmSettings.Mp4DecryptPath;
    }

    public bool SetMp4DecryptPath(string? value)
    {
        return SetProperty(DrmSettings.Mp4DecryptPath, value, v => DrmSettings.Mp4DecryptPath = v);
    }

    public bool GetAutoKeyAcquisition()
    {
        return DrmSettings.AutoKeyAcquisition;
    }

    public bool SetAutoKeyAcquisition(bool value)
    {
        return SetProperty(DrmSettings.AutoKeyAcquisition, value, v => DrmSettings.AutoKeyAcquisition = v);
    }

    public int GetDrmMaxRetryCount()
    {
        return DrmSettings.MaxRetryCount;
    }

    public bool SetDrmMaxRetryCount(int value)
    {
        return SetProperty(DrmSettings.MaxRetryCount, value, v => DrmSettings.MaxRetryCount = v);
    }

    public int GetRetryDelaySeconds()
    {
        return DrmSettings.RetryDelaySeconds;
    }

    public bool SetRetryDelaySeconds(int value)
    {
        return SetProperty(DrmSettings.RetryDelaySeconds, value, v => DrmSettings.RetryDelaySeconds = v);
    }

    public int GetRiskPauseMinutes()
    {
        return DrmSettings.RiskPauseMinutes;
    }

    public bool SetRiskPauseMinutes(int value)
    {
        return SetProperty(DrmSettings.RiskPauseMinutes, value, v => DrmSettings.RiskPauseMinutes = v);
    }

    public int GetContinuousFailureThreshold()
    {
        return DrmSettings.ContinuousFailureThreshold;
    }

    public bool SetContinuousFailureThreshold(int value)
    {
        return SetProperty(DrmSettings.ContinuousFailureThreshold, value, v => DrmSettings.ContinuousFailureThreshold = v);
    }

    public bool GetAutoCheckService()
    {
        return DrmSettings.AutoCheckService;
    }

    public bool SetAutoCheckService(bool value)
    {
        return SetProperty(DrmSettings.AutoCheckService, value, v => DrmSettings.AutoCheckService = v);
    }
}