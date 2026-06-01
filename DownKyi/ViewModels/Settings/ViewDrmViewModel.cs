using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Threading;
using DownKyi.Core.DRM;
using DownKyi.Core.Logging;
using DownKyi.Core.Settings;
using Prism.Commands;
using Prism.Events;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.ViewModels.Settings;

/// <summary>
/// DRM设置视图模型
/// </summary>
public class ViewDrmViewModel : ViewModelBase
{
    public const string Tag = "PageSettingsDrm";
    
    /// <summary>
    /// WVD文件路径
    /// </summary>
    private string _wvdFilePath = string.Empty;
    
    /// <summary>
    /// 是否启用DRM功能
    /// </summary>
    private bool _isDrmEnabled;
    
    /// <summary>
    /// 是否启用自动密钥获取
    /// </summary>
    private bool _autoKeyAcquisition;
    
    /// <summary>
    /// mp4decrypt路径
    /// </summary>
    private string _mp4DecryptPath = string.Empty;
    
    /// <summary>
    /// 是否启动时自动检测环境
    /// </summary>
    private bool _autoCheckService = true;
    
    /// <summary>
    /// 风控暂停时间（分钟）
    /// </summary>
    private int _riskPauseMinutes = 10;
    
    /// <summary>
    /// 连续失败阈值
    /// </summary>
    private int _continuousFailureThreshold = 3;
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    private int _maxRetryCount = 3;
    
    /// <summary>
    /// 重试延迟秒数
    /// </summary>
    private int _retryDelaySeconds = 300;
    
    /// <summary>
    /// WVD文件状态消息
    /// </summary>
    private string _wvdStatusMessage = string.Empty;
    
    /// <summary>
    /// 是否显示WVD状态
    /// </summary>
    private bool _wvdStatusVisible;
    
    /// <summary>
    /// 解密器状态
    /// </summary>
    private string _decryptorStatus = string.Empty;
    
    /// <summary>
    /// 是否显示解密器状态
    /// </summary>
    private bool _decryptorStatusVisible;
    
    /// <summary>
    /// DRM队列状态
    /// </summary>
    private string _drmQueueStatus = "DRM队列：空闲";
    
    /// <summary>
    /// WVD文件状态
    /// </summary>
    private string _wvdFileStatus = "WVD文件：未加载";
    
    /// <summary>
    /// 解密器可用性
    /// </summary>
    private string _decryptorAvailability = "解密器：检查中...";
    
    /// <summary>
    /// 环境检测状态
    /// </summary>
    private string _environmentStatus = "环境：检测中...";
    
    /// <summary>
    /// 环境是否正常
    /// </summary>
    private bool _isEnvironmentReady = false;
    
    /// <summary>
    /// Edge浏览器是否可用
    /// </summary>
    private bool _isEdgeAvailable = false;
    
    /// <summary>
    /// Python环境是否可用
    /// </summary>
    private bool _isPythonAvailable = false;
    
    /// <summary>
    /// Edge状态描述
    /// </summary>
    private string _edgeStatus = "Edge浏览器：检测中...";
    
    /// <summary>
    /// Python状态描述
    /// </summary>
    private string _pythonStatus = "Python环境：检测中...";
    
    public ViewDrmViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
    {
        // 加载设置
        LoadSettings();
        
        // 初始化DRM组件
        InitializeDrmComponents();
    }
    
    #region 属性
    
    /// <summary>
    /// WVD文件路径
    /// </summary>
    public string WvdFilePath
    {
        get => _wvdFilePath;
        set
        {
            if (SetProperty(ref _wvdFilePath, value))
            {
                // 保存设置
                SettingsManager.GetInstance().SetWvdFilePath(value);
            }
        }
    }
    
    /// <summary>
    /// 是否启用DRM功能
    /// </summary>
    public bool IsDrmEnabled
    {
        get => _isDrmEnabled;
        set
        {
            if (SetProperty(ref _isDrmEnabled, value))
            {
                SettingsManager.GetInstance().SetDrmEnabled(value);
                LogManager.Info(Tag, $"DRM功能已{(value ? "启用" : "禁用")}");
            }
        }
    }
    
    /// <summary>
    /// 是否启用自动密钥获取
    /// </summary>
    public bool AutoKeyAcquisition
    {
        get => _autoKeyAcquisition;
        set
        {
            if (SetProperty(ref _autoKeyAcquisition, value))
            {
                SettingsManager.GetInstance().SetAutoKeyAcquisition(value);
                LogManager.Info(Tag, $"自动密钥获取已{(value ? "启用" : "禁用")}");
            }
        }
    }
    
    /// <summary>
    /// mp4decrypt路径
    /// </summary>
    public string Mp4DecryptPath
    {
        get => _mp4DecryptPath;
        set
        {
            if (SetProperty(ref _mp4DecryptPath, value))
            {
                SettingsManager.GetInstance().SetMp4DecryptPath(value);
                // 重新初始化解密器
                DrmDecryptor.Initialize(value, null);
                UpdateDecryptorStatus();
            }
        }
    }
    
    /// <summary>
    /// 是否启动时自动检测环境
    /// </summary>
    public bool AutoCheckService
    {
        get => _autoCheckService;
        set
        {
            if (SetProperty(ref _autoCheckService, value))
            {
                SettingsManager.GetInstance().SetAutoCheckService(value);
                LogManager.Info(Tag, $"自动检测环境已{(value ? "启用" : "禁用")}");
            }
        }
    }
    
    /// <summary>
    /// 风控暂停时间（分钟）
    /// </summary>
    public int RiskPauseMinutes
    {
        get => _riskPauseMinutes;
        set
        {
            if (SetProperty(ref _riskPauseMinutes, value))
            {
                SettingsManager.GetInstance().SetRiskPauseMinutes(value);
            }
        }
    }
    
    /// <summary>
    /// 连续失败阈值
    /// </summary>
    public int ContinuousFailureThreshold
    {
        get => _continuousFailureThreshold;
        set
        {
            if (SetProperty(ref _continuousFailureThreshold, value))
            {
                SettingsManager.GetInstance().SetContinuousFailureThreshold(value);
            }
        }
    }
    
    /// <summary>
    /// 环境检测状态
    /// </summary>
    public string EnvironmentStatus
    {
        get => _environmentStatus;
        set
        {
            SetProperty(ref _environmentStatus, value);
        }
    }
    
    /// <summary>
    /// 环境是否正常
    /// </summary>
    public bool IsEnvironmentReady
    {
        get => _isEnvironmentReady;
        set
        {
            SetProperty(ref _isEnvironmentReady, value);
        }
    }
    
    /// <summary>
    /// Edge浏览器是否可用
    /// </summary>
    public bool IsEdgeAvailable
    {
        get => _isEdgeAvailable;
        set
        {
            SetProperty(ref _isEdgeAvailable, value);
        }
    }
    
    /// <summary>
    /// Python环境是否可用
    /// </summary>
    public bool IsPythonAvailable
    {
        get => _isPythonAvailable;
        set
        {
            SetProperty(ref _isPythonAvailable, value);
        }
    }
    
    /// <summary>
    /// Edge状态描述
    /// </summary>
    public string EdgeStatus
    {
        get => _edgeStatus;
        set
        {
            SetProperty(ref _edgeStatus, value);
        }
    }
    
    /// <summary>
    /// Python状态描述
    /// </summary>
    public string PythonStatus
    {
        get => _pythonStatus;
        set
        {
            SetProperty(ref _pythonStatus, value);
        }
    }
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount
    {
        get => _maxRetryCount;
        set
        {
            if (SetProperty(ref _maxRetryCount, value))
            {
                SettingsManager.GetInstance().SetDrmMaxRetryCount(value);
            }
        }
    }
    
    /// <summary>
    /// 重试延迟秒数
    /// </summary>
    public int RetryDelaySeconds
    {
        get => _retryDelaySeconds;
        set => SetProperty(ref _retryDelaySeconds, value);
    }
    
    /// <summary>
    /// WVD文件状态消息
    /// </summary>
    public string WvdStatusMessage
    {
        get => _wvdStatusMessage;
        set => SetProperty(ref _wvdStatusMessage, value);
    }
    
    /// <summary>
    /// 是否显示WVD状态
    /// </summary>
    public bool WvdStatusVisible
    {
        get => _wvdStatusVisible;
        set => SetProperty(ref _wvdStatusVisible, value);
    }
    
    /// <summary>
    /// 解密器状态
    /// </summary>
    public string DecryptorStatus
    {
        get => _decryptorStatus;
        set => SetProperty(ref _decryptorStatus, value);
    }
    
    /// <summary>
    /// 是否显示解密器状态
    /// </summary>
    public bool DecryptorStatusVisible
    {
        get => _decryptorStatusVisible;
        set => SetProperty(ref _decryptorStatusVisible, value);
    }
    
    /// <summary>
    /// DRM队列状态
    /// </summary>
    public string DrmQueueStatus
    {
        get => _drmQueueStatus;
        set => SetProperty(ref _drmQueueStatus, value);
    }
    
    /// <summary>
    /// WVD文件状态
    /// </summary>
    public string WvdFileStatus
    {
        get => _wvdFileStatus;
        set => SetProperty(ref _wvdFileStatus, value);
    }
    
    /// <summary>
    /// 解密器可用性
    /// </summary>
    public string DecryptorAvailability
    {
        get => _decryptorAvailability;
        set => SetProperty(ref _decryptorAvailability, value);
    }
    
    #endregion
    
    #region 命令
    
    /// <summary>
    /// 浏览WVD文件命令
    /// </summary>
    private DelegateCommand? _browseWvdFileCommand;
    public DelegateCommand BrowseWvdFileCommand => _browseWvdFileCommand ??= new DelegateCommand(ExecuteBrowseWvdFileCommand);
    
    private async void ExecuteBrowseWvdFileCommand()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(App.Current.MainWindow);
            if (topLevel == null) return;
            
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择WVD文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("WVD文件")
                    {
                        Patterns = new[] { "*.wvd" }
                    }
                }
            });
            
            if (files.Count > 0)
            {
                var file = files[0];
                WvdFilePath = file.Path.LocalPath;
                
                // 自动验证WVD文件
                ValidateWvdFileCommand.Execute();
            }
        }
        catch (Exception ex)
        {
            LogManager.Error(Tag, $"浏览WVD文件失败：{ex.Message}");
            Console.PrintLine($"❌ 浏览文件失败：{ex.Message}");
        }
    }
    
    /// <summary>
    /// 验证WVD文件命令
    /// </summary>
    private DelegateCommand? _validateWvdFileCommand;
    public DelegateCommand ValidateWvdFileCommand => _validateWvdFileCommand ??= new DelegateCommand(ExecuteValidateWvdFileCommand);
    
    private void ExecuteValidateWvdFileCommand()
    {
        if (string.IsNullOrWhiteSpace(WvdFilePath))
        {
            WvdStatusMessage = "请先选择WVD文件";
            WvdStatusVisible = true;
            return;
        }
        
        var result = WvdFileManager.ValidateWvdFile(WvdFilePath);
        
        if (result.IsValid)
        {
            WvdStatusMessage = $"✅ WVD文件有效：{result.FileName} ({result.DeviceType}, {result.SecurityLevel})";
            WvdStatusVisible = true;
            WvdFileStatus = $"WVD文件：{result.FileName} ({result.DeviceType})";
            
            // 自动切换WVD
            WvdFileManager.SwitchWvdFile(WvdFilePath);
            
            Console.PrintLine($"✅ WVD文件验证成功：{result.FileName}");
        }
        else
        {
            WvdStatusMessage = $"❌ WVD文件无效：{result.ValidationError}";
            WvdStatusVisible = true;
            WvdFileStatus = "WVD文件：无效";
            
            Console.PrintLine($"❌ WVD文件验证失败：{result.ValidationError}");
        }
    }
    
    /// <summary>
    /// DRM启用命令
    /// </summary>
    private DelegateCommand? _drmEnabledCommand;
    public DelegateCommand DrmEnabledCommand => _drmEnabledCommand ??= new DelegateCommand(ExecuteDrmEnabledCommand);
    
    private void ExecuteDrmEnabledCommand()
    {
        if (IsDrmEnabled && !WvdFileManager.IsWvdLoaded)
        {
            Console.PrintLine("⚠️ 启用DRM功能前请先配置有效的WVD文件");
        }
    }
    
    /// <summary>
    /// 自动密钥获取命令
    /// </summary>
    private DelegateCommand? _autoKeyAcquisitionCommand;
    public DelegateCommand AutoKeyAcquisitionCommand => _autoKeyAcquisitionCommand ??= new DelegateCommand(ExecuteAutoKeyAcquisitionCommand);
    
    private void ExecuteAutoKeyAcquisitionCommand()
    {
        Console.PrintLine($"🔧 自动密钥获取：{(AutoKeyAcquisition ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 浏览mp4decrypt命令
    /// </summary>
    private DelegateCommand? _browseMp4DecryptCommand;
    public DelegateCommand BrowseMp4DecryptCommand => _browseMp4DecryptCommand ??= new DelegateCommand(ExecuteBrowseMp4DecryptCommand);
    
    private async void ExecuteBrowseMp4DecryptCommand()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(App.Current.MainWindow);
            if (topLevel == null) return;
            
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择mp4decrypt工具",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("可执行文件")
                    {
                        Patterns = new[] { "*.exe" }
                    }
                }
            });
            
            if (files.Count > 0)
            {
                var file = files[0];
                Mp4DecryptPath = file.Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            LogManager.Error(Tag, $"浏览mp4decrypt失败：{ex.Message}");
            Console.PrintLine($"❌ 浏览文件失败：{ex.Message}");
        }
    }
    
    /// <summary>
    /// 启动DRM队列命令
    /// </summary>
    private DelegateCommand? _startDrmQueueCommand;
    public DelegateCommand StartDrmQueueCommand => _startDrmQueueCommand ??= new DelegateCommand(ExecuteStartDrmQueueCommand);
    
    private void ExecuteStartDrmQueueCommand()
    {
        if (!WvdFileManager.IsWvdLoaded)
        {
            Console.PrintLine("❌ 请先加载有效的WVD文件");
            return;
        }
        
        if (!IsEnvironmentReady)
        {
            Console.PrintLine("❌ 环境检测未通过，请先检查Edge和Python环境");
            return;
        }
        
        DrmTaskManager.Start();
        DrmQueueStatus = $"DRM队列：运行中 ({DrmTaskManager.QueueCount}个任务)";
        
        Console.PrintLine("🚀 DRM队列已启动");
    }
    
    /// <summary>
    /// 停止DRM队列命令
    /// </summary>
    private DelegateCommand? _stopDrmQueueCommand;
    public DelegateCommand StopDrmQueueCommand => _stopDrmQueueCommand ??= new DelegateCommand(ExecuteStopDrmQueueCommand);
    
    private void ExecuteStopDrmQueueCommand()
    {
        DrmTaskManager.Stop();
        DrmQueueStatus = "DRM队列：已停止";
        
        Console.PrintLine("⏹️ DRM队列已停止");
    }
    
    /// <summary>
    /// 清空DRM队列命令
    /// </summary>
    private DelegateCommand? _clearDrmQueueCommand;
    public DelegateCommand ClearDrmQueueCommand => _clearDrmQueueCommand ??= new DelegateCommand(ExecuteClearDrmQueueCommand);
    
    private void ExecuteClearDrmQueueCommand()
    {
        DrmTaskManager.ClearQueue();
        DrmQueueStatus = "DRM队列：空闲";
        
        Console.PrintLine("🗑️ DRM队列已清空");
    }
    
    /// <summary>
    /// 检测环境命令
    /// </summary>
    private DelegateCommand? _checkServiceConnectionCommand;
    public DelegateCommand CheckServiceConnectionCommand => _checkServiceConnectionCommand ??= new DelegateCommand(ExecuteCheckEnvironmentCommand);
    
    private async void ExecuteCheckEnvironmentCommand()
    {
        try
        {
            EnvironmentStatus = "环境：检测中...";
            IsEnvironmentReady = false;
            IsEdgeAvailable = false;
            IsPythonAvailable = false;
            
            // 并行检测Edge和Python环境
            var edgeTask = CheckEdgeEnvironmentAsync();
            var pythonTask = CheckPythonEnvironmentAsync();
            
            await Task.WhenAll(edgeTask, pythonTask);
            
            // 综合判断
            if (IsEdgeAvailable && IsPythonAvailable)
            {
                EnvironmentStatus = "✅ Edge & Python 环境正常";
                IsEnvironmentReady = true;
                Console.PrintLine("✅ Edge & Python 环境正常");
            }
            else if (!IsEdgeAvailable && !IsPythonAvailable)
            {
                EnvironmentStatus = "❌ Edge和Python环境均不可用";
                IsEnvironmentReady = false;
                Console.PrintLine("❌ Edge和Python环境均不可用");
            }
            else if (!IsEdgeAvailable)
            {
                EnvironmentStatus = "❌ Edge环境不可用";
                IsEnvironmentReady = false;
                Console.PrintLine("❌ Edge环境不可用");
            }
            else
            {
                EnvironmentStatus = "❌ Python环境不可用";
                IsEnvironmentReady = false;
                Console.PrintLine("❌ Python环境不可用");
            }
        }
        catch (Exception ex)
        {
            EnvironmentStatus = "❌ 环境检测失败";
            IsEnvironmentReady = false;
            LogManager.Error(Tag, $"环境检测失败：{ex.Message}");
            Console.PrintLine($"❌ 环境检测失败：{ex.Message}");
        }
    }
    
    /// <summary>
    /// 检测Edge浏览器环境
    /// </summary>
    private async Task CheckEdgeEnvironmentAsync()
    {
        try
        {
            EdgeStatus = "Edge浏览器：检测中...";
            
            // 尝试初始化Edge浏览器
            using var browser = new EdgeDrmBrowser();
            var success = await browser.InitializeAsync();
            
            if (success)
            {
                EdgeStatus = "✅ Edge浏览器：正常";
                IsEdgeAvailable = true;
                LogManager.Info(Tag, "Edge浏览器检测通过");
            }
            else
            {
                EdgeStatus = "❌ Edge浏览器：未检测到";
                IsEdgeAvailable = false;
                LogManager.Info(Tag, "未检测到系统Microsoft Edge浏览器");
            }
        }
        catch (Exception ex)
        {
            EdgeStatus = "❌ Edge浏览器：检测失败";
            IsEdgeAvailable = false;
            LogManager.Error(Tag, $"Edge浏览器检测失败：{ex.Message}");
        }
    }
    
    /// <summary>
    /// 检测Python环境
    /// </summary>
    private async Task CheckPythonEnvironmentAsync()
    {
        try
        {
            PythonStatus = "Python环境：检测中...";
            
            // 初始化Python环境
            var success = PythonDrmHelper.Initialize();
            
            if (success)
            {
                PythonStatus = "✅ Python环境：正常";
                IsPythonAvailable = true;
                LogManager.Info(Tag, "Python环境检测通过");
            }
            else
            {
                PythonStatus = "❌ Python环境：缺失";
                IsPythonAvailable = false;
                LogManager.Info(Tag, "嵌入式Python环境缺失");
            }
        }
        catch (Exception ex)
        {
            PythonStatus = "❌ Python环境：检测失败";
            IsPythonAvailable = false;
            LogManager.Error(Tag, $"Python环境检测失败：{ex.Message}");
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 强制解除风控命令
    /// </summary>
    private DelegateCommand? _forceResumeRiskCommand;
    public DelegateCommand ForceResumeRiskCommand => _forceResumeRiskCommand ??= new DelegateCommand(ExecuteForceResumeRiskCommand);
    
    private void ExecuteForceResumeRiskCommand()
    {
        DrmTaskManager.ForceResume();
        Console.PrintLine("▶️ 风控已强制解除");
    }
    
    #endregion
    
    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        var settings = SettingsManager.GetInstance();
        
        _wvdFilePath = settings.GetWvdFilePath() ?? "";
        _isDrmEnabled = settings.GetDrmEnabled();
        _autoKeyAcquisition = settings.GetAutoKeyAcquisition();
        _mp4DecryptPath = settings.GetMp4DecryptPath() ?? "";
        _autoCheckService = settings.GetAutoCheckService();
        _riskPauseMinutes = settings.GetRiskPauseMinutes();
        _continuousFailureThreshold = settings.GetContinuousFailureThreshold();
        _maxRetryCount = settings.GetDrmMaxRetryCount();
        
        RaisePropertyChanged(nameof(WvdFilePath));
        RaisePropertyChanged(nameof(IsDrmEnabled));
        RaisePropertyChanged(nameof(AutoKeyAcquisition));
        RaisePropertyChanged(nameof(Mp4DecryptPath));
        RaisePropertyChanged(nameof(AutoCheckService));
        RaisePropertyChanged(nameof(RiskPauseMinutes));
        RaisePropertyChanged(nameof(ContinuousFailureThreshold));
        RaisePropertyChanged(nameof(MaxRetryCount));
    }
    
    /// <summary>
    /// 初始化DRM组件
    /// </summary>
    private void InitializeDrmComponents()
    {
        // 初始化DRM解密器
        DrmDecryptor.Initialize(Mp4DecryptPath, null);
        
        // 加载WVD文件
        if (!string.IsNullOrWhiteSpace(WvdFilePath) && IsDrmEnabled)
        {
            WvdFileManager.LoadWvdFileOnStartup(WvdFilePath);
        }
        
        // 自动检测环境
        if (IsDrmEnabled && AutoCheckService)
        {
            // 延迟检测，避免阻塞启动
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                ExecuteCheckEnvironmentCommand();
            });
        }
        
        // 更新状态显示
        UpdateDrmStatus();
        UpdateDecryptorStatus();
    }
    
    /// <summary>
    /// 更新DRM状态
    /// </summary>
    private void UpdateDrmStatus()
    {
        // WVD文件状态
        WvdFileStatus = WvdFileManager.GetStatusDescription();
        
        // DRM队列状态
        var stats = DrmTaskManager.GetStatistics();
        var statusParts = new System.Collections.Generic.List<string>();
        
        statusParts.Add($"DRM队列：{(DrmTaskManager.IsRunning ? "运行中" : "未运行")}");
        
        if (DrmTaskManager.IsRiskPaused)
        {
            statusParts.Add("⚠️ 风控暂停中");
        }
        
        if (stats.queued > 0)
        {
            statusParts.Add($"{stats.queued}个任务等待");
        }
        
        if (DrmTaskManager.IsRiskPaused)
        {
            DrmQueueStatus = string.Join(" - ", statusParts);
        }
        else
        {
            DrmQueueStatus = $"DRM队列：{(DrmTaskManager.IsRunning ? "运行中" : "空闲")}";
            if (stats.queued > 0)
            {
                DrmQueueStatus = $"DRM队列：{stats.queued}个任务等待";
            }
        }
    }
    
    /// <summary>
    /// 更新解密器状态
    /// </summary>
    private void UpdateDecryptorStatus()
    {
        if (DrmDecryptor.IsDecryptorAvailable)
        {
            DecryptorAvailability = "解密器：可用";
            DecryptorStatus = $"mp4decrypt路径：{DrmDecryptor.GetStatusInfo().Split('\n')[0]}";
            DecryptorStatusVisible = true;
        }
        else
        {
            DecryptorAvailability = "解密器：不可用（请配置mp4decrypt）";
            DecryptorStatus = "警告：mp4decrypt工具未找到，DRM解密功能不可用";
            DecryptorStatusVisible = true;
        }
    }
    
    /// <summary>
    /// 刷新状态（供UI调用）
    /// </summary>
    public void RefreshStatus()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateDrmStatus();
            UpdateDecryptorStatus();
        });
    }
}
