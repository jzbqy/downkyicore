using DownKyi.Core.Logging;
using DownKyi.Models;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.Core.DRM;

public static class WvdFileManager
{
    private static readonly object LockObj = new();
    private static WvdFileInfo? _currentWvdFile;
    private static bool _isWvdLoaded;

    public static bool IsWvdLoaded => _isWvdLoaded;

    public static WvdFileInfo? CurrentWvdFile => _currentWvdFile;

    public static WvdValidationResult ValidateWvdFile(string filePath)
    {
        var result = new WvdValidationResult();
        
        try
        {
            if (!File.Exists(filePath))
            {
                result.IsValid = false;
                result.ValidationError = "文件不存在";
                return result;
            }

            var fileInfo = new FileInfo(filePath);
            result.FileName = fileInfo.Name;

            if (fileInfo.Length < 1024)
            {
                result.IsValid = false;
                result.ValidationError = "文件过小，不是有效的WVD文件";
                return result;
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var header = new byte[8];
            stream.Read(header, 0, 8);
            
            var headerStr = BitConverter.ToString(header).Replace("-", "");
            if (!headerStr.StartsWith("4D534346"))
            {
                result.IsValid = false;
                result.ValidationError = "文件格式不正确，缺少WVD文件头";
                return result;
            }

            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            try
            {
                var magic = reader.ReadUInt32();
                var version = reader.ReadUInt32();
                var deviceTypeLength = reader.ReadUInt32();
                var deviceTypeBytes = reader.ReadBytes((int)deviceTypeLength);
                var deviceType = System.Text.Encoding.UTF8.GetString(deviceTypeBytes);
                
                result.DeviceType = deviceType;
                result.SecurityLevel = "L3";
                result.IsValid = true;
                result.FileInfo = new WvdFileInfo
                {
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    DeviceType = deviceType,
                    SecurityLevel = WvdSecurityLevel.L3,
                    IsValid = true
                };

                LogManager.Info("WvdFileManager", $"WVD文件验证成功: {fileInfo.Name}, 设备类型: {deviceType}");
                Console.PrintLine($"✅ WVD文件验证成功: {fileInfo.Name}");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationError = $"解析WVD文件失败: {ex.Message}";
                LogManager.Error("WvdFileManager", $"WVD文件解析失败: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationError = $"验证失败: {ex.Message}";
            LogManager.Error("WvdFileManager", $"WVD文件验证异常: {ex.Message}");
        }

        return result;
    }

    public static bool LoadWvdFileOnStartup(string filePath)
    {
        lock (LockObj)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _isWvdLoaded = false;
                _currentWvdFile = null;
                return false;
            }

            var result = ValidateWvdFile(filePath);
            if (result.IsValid && result.FileInfo != null)
            {
                _currentWvdFile = result.FileInfo;
                _isWvdLoaded = true;
                LogManager.Info("WvdFileManager", $"WVD文件已加载: {result.FileName}");
                Console.PrintLine($"✅ WVD文件已加载: {result.FileName}");
                return true;
            }

            _isWvdLoaded = false;
            _currentWvdFile = null;
            return false;
        }
    }

    public static bool SwitchWvdFile(string filePath)
    {
        lock (LockObj)
        {
            var result = ValidateWvdFile(filePath);
            if (result.IsValid && result.FileInfo != null)
            {
                _currentWvdFile = result.FileInfo;
                _isWvdLoaded = true;
                LogManager.Info("WvdFileManager", $"WVD文件已切换: {result.FileName}");
                Console.PrintLine($"🔄 WVD文件已切换: {result.FileName}");
                return true;
            }

            return false;
        }
    }

    public static string GetStatusDescription()
    {
        lock (LockObj)
        {
            if (_isWvdLoaded && _currentWvdFile != null)
            {
                return $"WVD文件：{_currentWvdFile.FileName} ({_currentWvdFile.DeviceType})";
            }
            return "WVD文件：未加载";
        }
    }

    public static void UnloadWvdFile()
    {
        lock (LockObj)
        {
            _currentWvdFile = null;
            _isWvdLoaded = false;
            LogManager.Info("WvdFileManager", "WVD文件已卸载");
        }
    }
}