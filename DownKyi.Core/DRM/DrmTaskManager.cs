using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DownKyi.Core.Logging;
using DownKyi.Models;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.Core.DRM;

public static class DrmTaskManager
{
    private static readonly ConcurrentQueue<DrmTaskInfo> _taskQueue = new();
    private static readonly object _lockObj = new();
    private static bool _isRunning;
    private static bool _isRiskPaused;
    private static int _continuousFailures;
    private static Task? _workerTask;
    private static int _maxRetryCount = 3;
    private static int _continuousFailureThreshold = 3;
    private static int _riskPauseMinutes = 10;

    public static bool IsRunning => _isRunning;
    public static bool IsRiskPaused => _isRiskPaused;
    public static int QueueCount => _taskQueue.Count;

    public static void Start()
    {
        lock (_lockObj)
        {
            if (_isRunning) return;
            _isRunning = true;
            _isRiskPaused = false;
            _continuousFailures = 0;

            _workerTask = Task.Run(ProcessQueueAsync);
            LogManager.Info("DrmTaskManager", "DRM任务管理器已启动");
            Console.PrintLine("🚀 DRM任务管理器已启动");
        }
    }

    public static void Stop()
    {
        lock (_lockObj)
        {
            _isRunning = false;
        }

        if (_workerTask != null)
        {
            try
            {
                _workerTask.Wait(10000);
            }
            catch
            {
                // ignored
            }
        }

        LogManager.Info("DrmTaskManager", "DRM任务管理器已停止");
        Console.PrintLine("⏹️ DRM任务管理器已停止");
    }

    public static void AddTask(DrmTaskInfo task)
    {
        _taskQueue.Enqueue(task);
        LogManager.Info("DrmTaskManager", $"任务已加入队列: {task.TaskId}");
    }

    public static void RemoveTask(string taskId)
    {
        // ConcurrentQueue不支持直接移除，需要重建队列
        var newQueue = new ConcurrentQueue<DrmTaskInfo>();
        while (_taskQueue.TryDequeue(out var task))
        {
            if (task.TaskId != taskId)
            {
                newQueue.Enqueue(task);
            }
        }

        while (newQueue.TryDequeue(out var task))
        {
            _taskQueue.Enqueue(task);
        }
    }

    public static void ClearQueue()
    {
        while (_taskQueue.TryDequeue(out _)) { }
        LogManager.Info("DrmTaskManager", "任务队列已清空");
    }

    public static void ForceResume()
    {
        lock (_lockObj)
        {
            _isRiskPaused = false;
            _continuousFailures = 0;
        }
        LogManager.Info("DrmTaskManager", "风控已强制解除");
        Console.PrintLine("▶️ 风控已强制解除");
    }

    public static (int queued, int completed, int failed) GetStatistics()
    {
        return (_taskQueue.Count, 0, 0);
    }

    public static void SetMaxRetryCount(int count)
    {
        _maxRetryCount = count;
    }

    public static void SetContinuousFailureThreshold(int threshold)
    {
        _continuousFailureThreshold = threshold;
    }

    public static void SetRiskPauseMinutes(int minutes)
    {
        _riskPauseMinutes = minutes;
    }

    private static async Task ProcessQueueAsync()
    {
        while (_isRunning)
        {
            if (_isRiskPaused)
            {
                await Task.Delay(60000);
                continue;
            }

            if (!_taskQueue.TryDequeue(out var task))
            {
                await Task.Delay(1000);
                continue;
            }

            await ProcessTaskAsync(task);
        }
    }

    private static async Task ProcessTaskAsync(DrmTaskInfo task)
    {
        try
        {
            var random = new Random();
            await Task.Delay(random.Next(1000, 5000));

            task.Status = DrmStatus.AcquiringKey;
            
            var edgeBrowser = new EdgeDrmBrowser();
            var license = await edgeBrowser.GetDrmLicense(task.VideoUrl, task.Pssh);
            
            if (license == null)
            {
                throw new Exception("无法获取DRM许可证");
            }

            if (!WvdFileManager.IsWvdLoaded || WvdFileManager.CurrentWvdFile == null)
            {
                throw new Exception("WVD文件未加载");
            }

            var keysJson = PythonDrmHelper.ParseLicense(license, WvdFileManager.CurrentWvdFile.FilePath);
            if (string.IsNullOrWhiteSpace(keysJson))
            {
                throw new Exception("无法解析DRM密钥");
            }

            task.Status = DrmStatus.Decrypting;

            var keys = ParseKeysFromJson(keysJson);
            if (keys == null || keys.Count == 0)
            {
                throw new Exception("未解析到有效的密钥");
            }

            var inputFile = task.OutputPath + ".encrypted";
            var outputFile = task.OutputPath;

            if (!DrmDecryptor.DecryptFile(inputFile, outputFile, keys))
            {
                throw new Exception("解密失败");
            }

            task.Status = DrmStatus.Completed;
            task.CompleteTime = DateTime.Now;
            _continuousFailures = 0;

            LogManager.Info("DrmTaskManager", $"任务完成: {task.TaskId}");
            Console.PrintLine($"✅ DRM任务完成: {task.OutputFileName}");
        }
        catch (Exception ex)
        {
            task.Status = DrmStatus.Failed;
            task.ErrorType = DrmErrorType.KeyAcquisitionFailed;
            task.ErrorMessage = ex.Message;
            task.RetryCount++;

            _continuousFailures++;
            LogManager.Error("DrmTaskManager", $"任务失败: {task.TaskId}, 错误: {ex.Message}");
            Console.PrintLine($"❌ DRM任务失败: {task.OutputFileName} - {ex.Message}");

            if (_continuousFailures >= _continuousFailureThreshold)
            {
                _isRiskPaused = true;
                LogManager.Warning("DrmTaskManager", $"连续失败{_continuousFailures}次，触发风控暂停");
                Console.PrintLine($"⚠️ 连续失败{_continuousFailures}次，触发风控暂停{_riskPauseMinutes}分钟");
            }

            if (task.RetryCount < _maxRetryCount)
            {
                _taskQueue.Enqueue(task);
                LogManager.Info("DrmTaskManager", $"任务重新入队，重试次数: {task.RetryCount}");
            }
        }
    }

    private static List<(string kid, string key)>? ParseKeysFromJson(string json)
    {
        try
        {
            var keys = new List<(string, string)>();
            var regex = new System.Text.RegularExpressions.Regex(@"""kid""\s*:\s*""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            var keyMatches = regex.Matches(json);
            var keyRegex = new System.Text.RegularExpressions.Regex(@"""key""\s*:\s*""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            var valueMatches = keyRegex.Matches(json);

            var count = Math.Min(keyMatches.Count, valueMatches.Count);
            for (int i = 0; i < count; i++)
            {
                keys.Add((keyMatches[i].Groups[1].Value, valueMatches[i].Groups[1].Value));
            }

            return keys;
        }
        catch
        {
            return null;
        }
    }
}