namespace OpenCodeSleepGuard;

using System;
using System.Runtime.InteropServices;

public sealed class SleepManager : IDisposable
{
    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private bool _isSleepPrevented;

    public bool IsSleepPrevented => _isSleepPrevented;

    public SleepManager()
    {
        _isSleepPrevented = false;
    }

    public void PreventSleep()
    {
        if (_isSleepPrevented)
        {
            Console.WriteLine("[SleepManager] PreventSleep called but sleep is already prevented — no-op");
            return;
        }

        uint result = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
        if (result == 0)
        {
            Console.WriteLine("[SleepManager] PreventSleep failed — SetThreadExecutionState returned 0");
            return;
        }

        _isSleepPrevented = true;
        Console.WriteLine("[SleepManager] Sleep prevented");
    }

    public void AllowSleep()
    {
        if (!_isSleepPrevented)
        {
            Console.WriteLine("[SleepManager] AllowSleep called but sleep is not prevented — no-op");
            return;
        }

        uint result = SetThreadExecutionState(ES_CONTINUOUS);
        if (result == 0)
        {
            Console.WriteLine("[SleepManager] AllowSleep failed — SetThreadExecutionState returned 0");
            return;
        }

        _isSleepPrevented = false;
        Console.WriteLine("[SleepManager] Sleep allowed");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isSleepPrevented)
        {
            uint result = SetThreadExecutionState(ES_CONTINUOUS);
            if (result != 0)
            {
                Console.WriteLine("[SleepManager] Dispose: sleep restored");
            }
            else
            {
                Console.WriteLine("[SleepManager] Dispose: failed to restore sleep");
            }
            _isSleepPrevented = false;
        }
    }

    ~SleepManager()
    {
        Dispose(false);
    }
}
