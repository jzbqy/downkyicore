using DownKyi.Core.Logging;
using Microsoft.Playwright;

namespace DownKyi.Core.DRM;

public sealed class PlaywrightSingleton
{
    private static readonly Lazy<PlaywrightSingleton> LazyInstance = new(() => new PlaywrightSingleton());
    
    private IPlaywright? _playwright;
    private readonly object _lockObj = new();
    private bool _isInitialized;

    public static PlaywrightSingleton Instance => LazyInstance.Value;

    private PlaywrightSingleton() { }

    public async Task<IPlaywright> GetPlaywrightAsync()
    {
        if (_playwright != null)
        {
            return _playwright;
        }

        lock (_lockObj)
        {
            if (_playwright != null)
            {
                return _playwright;
            }
        }

        try
        {
            _playwright = await Playwright.CreateAsync();
            _isInitialized = true;
            LogManager.Info("PlaywrightSingleton", "Playwright实例创建成功");
            return _playwright;
        }
        catch (Exception ex)
        {
            LogManager.Error("PlaywrightSingleton", $"Playwright初始化失败: {ex.Message}");
            throw;
        }
    }

    public bool IsInitialized => _isInitialized;

    public void Dispose()
    {
        if (_playwright != null)
        {
            try
            {
                _playwright.Dispose();
                _playwright = null;
                _isInitialized = false;
                LogManager.Info("PlaywrightSingleton", "Playwright实例已释放");
            }
            catch
            {
                // ignored
            }
        }
    }
}