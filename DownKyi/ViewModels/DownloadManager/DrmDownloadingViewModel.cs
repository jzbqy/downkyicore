using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DownKyi.Core.DRM;
using DownKyi.Core.Logging;
using DownKyi.Models;
using Prism.Commands;
using Prism.Events;

namespace DownKyi.ViewModels.DownloadManager;

public class DrmDownloadingViewModel : ViewModelBase
{
    public const string Tag = "DrmDownloadingViewModel";
    
    private ObservableCollection<DrmTaskInfo> _drmTasks = new();
    public ObservableCollection<DrmTaskInfo> DrmTasks
    {
        get => _drmTasks;
        set => SetProperty(ref _drmTasks, value);
    }

    private string _queueStatus = "DRM队列：空闲";
    public string QueueStatus
    {
        get => _queueStatus;
        set => SetProperty(ref _queueStatus, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    private bool _isRiskPaused;
    public bool IsRiskPaused
    {
        get => _isRiskPaused;
        set => SetProperty(ref _isRiskPaused, value);
    }

    public DrmDownloadingViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
    {
        InitializeCommands();
        StartStatusMonitor();
    }

    private void InitializeCommands()
    {
        StartQueueCommand = new DelegateCommand(ExecuteStartQueue);
        StopQueueCommand = new DelegateCommand(ExecuteStopQueue);
        ClearQueueCommand = new DelegateCommand(ExecuteClearQueue);
        ForceResumeCommand = new DelegateCommand(ExecuteForceResume);
    }

    public DelegateCommand StartQueueCommand { get; private set; }
    public DelegateCommand StopQueueCommand { get; private set; }
    public DelegateCommand ClearQueueCommand { get; private set; }
    public DelegateCommand ForceResumeCommand { get; private set; }

    private void ExecuteStartQueue()
    {
        if (!WvdFileManager.IsWvdLoaded)
        {
            Console.PrintLine("❌ 请先加载有效的WVD文件");
            return;
        }
        
        DrmTaskManager.Start();
        IsRunning = true;
        Console.PrintLine("🚀 DRM队列已启动");
    }

    private void ExecuteStopQueue()
    {
        DrmTaskManager.Stop();
        IsRunning = false;
        Console.PrintLine("⏹️ DRM队列已停止");
    }

    private void ExecuteClearQueue()
    {
        DrmTaskManager.ClearQueue();
        DrmTasks.Clear();
        Console.PrintLine("🗑️ DRM队列已清空");
    }

    private void ExecuteForceResume()
    {
        DrmTaskManager.ForceResume();
        IsRiskPaused = false;
        Console.PrintLine("▶️ 风控已强制解除");
    }

    private void StartStatusMonitor()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000);
                
                IsRunning = DrmTaskManager.IsRunning;
                IsRiskPaused = DrmTaskManager.IsRiskPaused;
                
                UpdateQueueStatus();
            }
        });
    }

    private void UpdateQueueStatus()
    {
        var stats = DrmTaskManager.GetStatistics();
        
        if (IsRiskPaused)
        {
            QueueStatus = $"DRM队列：⚠️ 风控暂停中 ({stats.queued}个任务等待)";
        }
        else if (IsRunning)
        {
            QueueStatus = $"DRM队列：运行中 ({stats.queued}个任务等待)";
        }
        else
        {
            QueueStatus = stats.queued > 0 
                ? $"DRM队列：{stats.queued}个任务等待" 
                : "DRM队列：空闲";
        }
    }
}