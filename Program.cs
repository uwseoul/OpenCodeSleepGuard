using System;
using System.Threading;
using System.Windows.Forms;

namespace OpenCodeSleepGuard;

public static class Program
{
    private static AppSettings _settings = null!;
    private static SleepManager _sleepManager = null!;
    private static ProcessWatcher _processWatcher = null!;
    private static OpenCodeDbMonitor _dbMonitor = null!;
    private static TrayIcon _trayIcon = null!;
    private static StatusWindow _statusWindow = null!;
    private static readonly CancellationTokenSource _cts = new();

    [STAThread]
    public static void Main(string[] args)
    {
        // Handle CLI arguments
        if (args.Length > 0)
        {
            HandleCliArgs(args);
            return;
        }

        Console.WriteLine("[Program] OpenCodeSleepGuard starting...");

        // Load settings
        _settings = AppSettings.Load();
        Console.WriteLine($"[Program] Settings loaded: ProcessNames=[{string.Join(", ", _settings.ProcessNames)}], " +
                          $"CheckInterval={_settings.CheckIntervalSeconds}s, DbPath={_settings.DbPath}");

        // Initialize components
        _sleepManager = new SleepManager();
        _processWatcher = new ProcessWatcher(_settings.ProcessNames);
        _dbMonitor = new OpenCodeDbMonitor(_settings.DbPath);

        // Setup tray icon on the main STA thread (required for WinForms)
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        _trayIcon = new TrayIcon();
        _trayIcon.ExitRequested += OnExitRequested;
        _trayIcon.StatusShowRequested += OnStatusShowRequested;

        // Subscribe to process state changes
        _processWatcher.ProcessStateChanged += OnProcessStateChanged;
        _statusWindow = new StatusWindow();

        // Setup Ctrl+C handler
        Console.CancelKeyPress += OnCancelKeyPress;

        // Run the main loop on a background thread
        var loopThread = new Thread(MainLoop)
        {
            Name = "SleepGuardLoop",
            IsBackground = true
        };
        loopThread.Start(_cts.Token);

        Console.WriteLine("[Program] Running. Press Ctrl+C or use tray icon to exit.");

        // Auto-show status window on startup
        _statusWindow.ShowStatus();

        // Run WinForms message pump (keeps tray icon alive)
        Application.Run();

        Cleanup();
    }

    private static void MainLoop(object? state)
    {
        var cancellationToken = (CancellationToken)state!;
        var checkInterval = TimeSpan.FromSeconds(_settings.CheckIntervalSeconds);
        var initialDbState = _dbMonitor.Initialize();

        Console.WriteLine($"[Program] Main loop started. DB state: {initialDbState.LastActivity}, Working={initialDbState.IsWorking}");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _processWatcher.RefreshState();
                var processes = _processWatcher.GetProcesses();
                var isRunning = processes.Count > 0;

                if (isRunning)
                {
                    var result = _dbMonitor.Poll();

                    if (result.IsWorking)
                    {
                        if (!_sleepManager.IsSleepPrevented)
                        {
                            _sleepManager.PreventSleep();
                            _trayIcon.SetWorking();
                            Console.WriteLine($"[Program] Working — DB event: {result.LastActivity} — sleep prevented");
                        }
                    }
                    else
                    {
                        _trayIcon.SetIdle();

                        if (_sleepManager.IsSleepPrevented)
                        {
                            _sleepManager.AllowSleep();
                            Console.WriteLine($"[Program] Idle — DB event: {result.LastActivity} — sleep allowed");
                        }
                    }

                    _statusWindow.UpdateStatus(
                        true,
                        result.IsWorking,
                        result.LastActivity,
                        result.LastActivityTime,
                        result.SessionTitle,
                        result.AgentName,
                        result.TaskInfo,
                        result.DbStatus,
                        _sleepManager.IsSleepPrevented);
                }
                else
                {
                    if (_sleepManager.IsSleepPrevented)
                    {
                        _sleepManager.AllowSleep();
                        Console.WriteLine("[Program] Target process not running — sleep restored");
                    }

                    _trayIcon.SetIdle();
                    _statusWindow.UpdateStatus(false, false, "프로세스 없음", null, "-", "-", "-", "DB 대기", false);

                    Console.WriteLine("[Program] Target process not detected — auto-exiting.");
                    Shutdown();
                    break;
                }

                Thread.Sleep(checkInterval);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Error in main loop: {ex.Message}");
                Thread.Sleep(checkInterval);
            }
        }

        Console.WriteLine("[Program] Main loop ended.");
    }

    private static void OnProcessStateChanged(object? sender, bool isRunning)
    {
        Console.WriteLine($"[Program] Process state changed: {(isRunning ? "detected" : "lost")}");

        if (!isRunning && _sleepManager.IsSleepPrevented)
        {
            _sleepManager.AllowSleep();
            _trayIcon.SetIdle();
        }
    }

    private static void OnExitRequested(object? sender, EventArgs e)
    {
        Console.WriteLine("[Program] Exit requested via tray icon.");
        Shutdown();
    }

    private static void OnStatusShowRequested(object? sender, EventArgs e)
    {
        Console.WriteLine("[Program] Status window requested.");
        _statusWindow.ShowStatus();
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        Console.WriteLine("[Program] Ctrl+C received.");
        Shutdown();
    }

    private static void Shutdown()
    {
        _cts.Cancel();
        Application.Exit();
    }

    private static void Cleanup()
    {
        Console.WriteLine("[Program] Cleaning up...");
        _dbMonitor?.Dispose();
        _sleepManager?.Dispose();
        _statusWindow?.Dispose();
        _trayIcon?.Dispose();
        _cts?.Dispose();
        Console.WriteLine("[Program] OpenCodeSleepGuard stopped.");
    }

    private static void HandleCliArgs(string[] args)
    {
        var arg = args[0].ToLowerInvariant().TrimStart('-', '/');

        switch (arg)
        {
            case "install":
                Console.WriteLine("[Program] Installing scheduled task...");
                if (TaskSchedulerManager.Install())
                {
                    Console.WriteLine("[Program] Scheduled task installed successfully.");
                }
                else
                {
                    Console.WriteLine("[Program] Failed to install scheduled task. Try running as Administrator.");
                }
                break;

            case "uninstall":
                Console.WriteLine("[Program] Removing scheduled task...");
                if (TaskSchedulerManager.Uninstall())
                {
                    Console.WriteLine("[Program] Scheduled task removed successfully.");
                }
                else
                {
                    Console.WriteLine("[Program] Failed to remove scheduled task.");
                }
                break;

            case "help":
            case "?":
                Console.WriteLine("OpenCodeSleepGuard — Prevents Windows sleep while OpenCode is working.");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  OpenCodeSleepGuard              Run the guard (background)");
                Console.WriteLine("  OpenCodeSleepGuard --install    Register as auto-start task");
                Console.WriteLine("  OpenCodeSleepGuard --uninstall  Remove auto-start task");
                Console.WriteLine("  OpenCodeSleepGuard --help       Show this help");
                break;

            default:
                Console.WriteLine($"Unknown argument: {args[0]}");
                Console.WriteLine("Use --help for usage information.");
                break;
        }
    }
}
