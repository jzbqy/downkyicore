using System.Diagnostics;
using DownKyi.Core.Logging;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.Core.DRM;

public static class DrmDecryptor
{
    private static string? _mp4DecryptPath;
    private static bool _isInitialized;
    private static bool _isDecryptorAvailable;

    public static bool IsDecryptorAvailable => _isDecryptorAvailable;

    public static void Initialize(string? mp4DecryptPath, string? tempPath = null)
    {
        _mp4DecryptPath = mp4DecryptPath;
        _isInitialized = true;
        
        if (!string.IsNullOrWhiteSpace(mp4DecryptPath) && File.Exists(mp4DecryptPath))
        {
            _isDecryptorAvailable = true;
            LogManager.Info("DrmDecryptor", $"解密器初始化成功: {mp4DecryptPath}");
            Console.PrintLine($"✅ mp4decrypt已配置: {Path.GetFileName(mp4DecryptPath)}");
        }
        else
        {
            _isDecryptorAvailable = false;
            LogManager.Warning("DrmDecryptor", "mp4decrypt未配置或不存在");
        }
    }

    public static bool DecryptFile(string inputFile, string outputFile, List<(string kid, string key)> keys)
    {
        if (!_isDecryptorAvailable || string.IsNullOrWhiteSpace(_mp4DecryptPath))
        {
            LogManager.Error("DrmDecryptor", "解密器不可用，请先配置mp4decrypt");
            return false;
        }

        try
        {
            var keyArgs = keys.Select(k => $"--key {k.kid}:{k.key}").ToList();
            var arguments = $"{string.Join(" ", keyArgs)} \"{inputFile}\" \"{outputFile}\"";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _mp4DecryptPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(_mp4DecryptPath)
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(60000);

            if (process.ExitCode == 0)
            {
                LogManager.Info("DrmDecryptor", $"解密成功: {Path.GetFileName(inputFile)}");
                Console.PrintLine($"✅ 解密完成: {Path.GetFileName(outputFile)}");
                return true;
            }
            
            LogManager.Error("DrmDecryptor", $"解密失败，退出码: {process.ExitCode}, 错误: {error}");
            Console.PrintLine($"❌ 解密失败: {error}");
            return false;
        }
        catch (Exception ex)
        {
            LogManager.Error("DrmDecryptor", $"解密异常: {ex.Message}");
            Console.PrintLine($"❌ 解密异常: {ex.Message}");
            return false;
        }
    }

    public static string GetStatusInfo()
    {
        if (!_isInitialized)
        {
            return "未初始化";
        }
        
        if (_isDecryptorAvailable && !string.IsNullOrWhiteSpace(_mp4DecryptPath))
        {
            return _mp4DecryptPath;
        }
        
        return "不可用";
    }

    public static bool AutoDetectMp4Decrypt()
    {
        var searchPaths = new[]
        {
            "tools/mp4decrypt.exe",
            "../tools/mp4decrypt.exe",
            "./mp4decrypt.exe",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "/Bento4/bin/mp4decrypt.exe",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "/Bento4/bin/mp4decrypt.exe"
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                Initialize(path);
                return true;
            }
        }

        return false;
    }
}