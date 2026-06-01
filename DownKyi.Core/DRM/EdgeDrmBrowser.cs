using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DownKyi.Core.Logging;
using Microsoft.Playwright;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.Core.DRM;

public class EdgeDrmBrowser : IDisposable
{
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _isDisposed;

    public async Task<bool> InitializeAsync()
    {
        try
        {
            if (!IsEdgeInstalled())
            {
                LogManager.Info("EdgeDrmBrowser", "未检测到Microsoft Edge浏览器");
                return false;
            }

            var playwright = await Playwright.CreateAsync();
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = "msedge",
                Args = new[]
                {
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-web-security",
                    "--allow-running-insecure-content",
                    "--disable-features=VizDisplayCompositor"
                }
            };

            _browser = await playwright.Chromium.LaunchAsync(launchOptions);
            LogManager.Info("EdgeDrmBrowser", "Edge浏览器初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            LogManager.Error("EdgeDrmBrowser", $"初始化失败: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> GetDrmLicense(string videoUrl, string pssh)
    {
        if (_browser == null)
        {
            await InitializeAsync();
            if (_browser == null)
            {
                return null;
            }
        }

        try
        {
            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();

            await _page.SetViewportSizeAsync(1920, 1080);
            await _page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0" },
                { "Referer", "https://www.bilibili.com/" }
            });

            var licenseCaptureTask = CaptureLicenseAsync();

            await _page.GotoAsync(videoUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Task.Delay(5000);

            var license = await licenseCaptureTask;
            return license;
        }
        catch (Exception ex)
        {
            LogManager.Error("EdgeDrmBrowser", $"获取DRM许可证失败: {ex.Message}");
            return null;
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private async Task<string?> CaptureLicenseAsync()
    {
        if (_page == null) return null;

        var licenseEvent = new TaskCompletionSource<string?>();

        _page.Request += (sender, request) =>
        {
            try
            {
                if (request.Url.Contains("license") || request.Url.Contains("getlicense"))
                {
                    var postData = request.PostData;
                    if (!string.IsNullOrEmpty(postData))
                    {
                        licenseEvent.TrySetResult(postData);
                    }
                }
            }
            catch
            {
                // ignored
            }
        };

        var timeoutTask = Task.Delay(30000).ContinueWith(_ =>
        {
            licenseEvent.TrySetResult(null);
        });

        return await Task.WhenAny(licenseEvent.Task, timeoutTask);
    }

    public static bool IsEdgeInstalled()
    {
        var edgePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe")
        };

        return edgePaths.Any(File.Exists);
    }

    private async Task CleanupAsync()
    {
        try
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }

            if (_context != null)
            {
                await _context.CloseAsync();
                _context = null;
            }
        }
        catch
        {
            // ignored
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            CleanupAsync().Wait(5000);
            
            if (_browser != null)
            {
                _browser.DisposeAsync().AsTask().Wait(5000);
                _browser = null;
            }
        }
        catch
        {
            // ignored
        }
    }
}
