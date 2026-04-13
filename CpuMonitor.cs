using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenCodeSleepGuard;

public class CpuMonitor
{
    private readonly Dictionary<int, (TimeSpan CpuTime, DateTime Timestamp)> _processHistory = new();

    public double GetCpuUsage(Process process)
    {
        if (process == null)
            return 0.0;

        int pid;
        try
        {
            pid = process.Id;
        }
        catch (Exception)
        {
            return 0.0;
        }

        TimeSpan currentCpuTime;
        DateTime currentTimestamp;

        try
        {
            currentCpuTime = process.TotalProcessorTime;
            currentTimestamp = DateTime.UtcNow;
        }
        catch (ObjectDisposedException)
        {
            return 0.0;
        }
        catch (InvalidOperationException)
        {
            return 0.0;
        }

        if (!_processHistory.TryGetValue(pid, out var previous))
        {
            _processHistory[pid] = (currentCpuTime, currentTimestamp);
            return 0.0;
        }

        TimeSpan deltaCpuTime = currentCpuTime - previous.CpuTime;
        TimeSpan deltaWallTime = currentTimestamp - previous.Timestamp;

        _processHistory[pid] = (currentCpuTime, currentTimestamp);

        if (deltaWallTime.TotalMilliseconds < 1)
            return 0.0;

        double cpuUsage = (deltaCpuTime.TotalMilliseconds / deltaWallTime.TotalMilliseconds / Environment.ProcessorCount) * 100.0;

        return Math.Min(100.0, Math.Max(0.0, cpuUsage));
    }

    public double GetTotalCpuUsage(IEnumerable<Process> processes)
    {
        if (processes == null)
            return 0.0;

        double totalUsage = 0.0;

        foreach (var process in processes)
        {
            totalUsage += GetCpuUsage(process);
        }

        return Math.Min(100.0, totalUsage);
    }
}
