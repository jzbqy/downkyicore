using System.Diagnostics;
using System.Text;
using DownKyi.Core.Logging;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.Core.DRM;

public static class PythonDrmHelper
{
    private static string? _pythonPath;
    private static string? _scriptPath;
    private static bool _isInitialized;

    public static bool Initialize()
    {
        var appDir = AppContext.BaseDirectory;
        
        var pythonEmbedPath = Path.Combine(appDir, "_python_embed", "python.exe");
        var scriptPath = Path.Combine(appDir, "Scripts", "drm_parse.py");

        if (!File.Exists(pythonEmbedPath))
        {
            pythonEmbedPath = Path.Combine(Directory.GetParent(appDir)?.FullName ?? appDir, "_python_embed", "python.exe");
        }

        if (!File.Exists(scriptPath))
        {
            scriptPath = Path.Combine(Directory.GetParent(appDir)?.FullName ?? appDir, "Scripts", "drm_parse.py");
        }

        if (!File.Exists(pythonEmbedPath))
        {
            LogManager.Warning("PythonDrmHelper", "未找到嵌入式Python环境");
            return false;
        }

        if (!File.Exists(scriptPath))
        {
            LogManager.Warning("PythonDrmHelper", "未找到drm_parse.py脚本");
            return false;
        }

        _pythonPath = pythonEmbedPath;
        _scriptPath = scriptPath;
        _isInitialized = true;

        LogManager.Info("PythonDrmHelper", $"Python环境初始化成功: {pythonEmbedPath}");
        Console.PrintLine($"✅ Python环境已配置");
        return true;
    }

    public static bool IsPythonAvailable()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        return !string.IsNullOrWhiteSpace(_pythonPath) && File.Exists(_pythonPath!);
    }

    public static string? ParseLicense(string licenseData, string wvdPath)
    {
        if (!_isInitialized || string.IsNullOrWhiteSpace(_pythonPath) || string.IsNullOrWhiteSpace(_scriptPath))
        {
            LogManager.Error("PythonDrmHelper", "Python环境未初始化");
            return null;
        }

        try
        {
            var arguments = $"\"{_scriptPath}\" --license \"{licenseData}\" --wvd \"{wvdPath}\"";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(_pythonPath)
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = process.WaitForExit(20000);

            if (!completed)
            {
                ForceKillProcess(process.Id);
                LogManager.Error("PythonDrmHelper", "Python脚本执行超时");
                return null;
            }

            if (process.ExitCode != 0)
            {
                LogManager.Error("PythonDrmHelper", $"Python脚本执行失败: {errorBuilder}");
                return null;
            }

            var output = outputBuilder.ToString();
            LogManager.Info("PythonDrmHelper", "许可证解析成功");
            return output;
        }
        catch (Exception ex)
        {
            LogManager.Error("PythonDrmHelper", $"解析许可证异常: {ex.Message}");
            return null;
        }
    }

    private static void ForceKillProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                process.Kill(true);
                process.WaitForExit(5000);
                LogManager.Info("PythonDrmHelper", $"强制终止进程: {processId}");
            }
        }
        catch
        {
            // ignored
        }
    }

    public static void ForceKillPythonProcesses()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("python"))
            {
                try
                {
                    process.Kill(true);
                    process.WaitForExit(5000);
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch
        {
            // ignored
        }
    }
}