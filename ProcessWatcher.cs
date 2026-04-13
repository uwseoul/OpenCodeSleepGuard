using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpenCodeSleepGuard
{
    public class ProcessWatcher
    {
        private readonly List<string> _processNames;
        private bool _isRunning;
        private bool _hasBeenInitialized;

        public ProcessWatcher(List<string> processNames)
        {
            _processNames = processNames ?? throw new ArgumentNullException(nameof(processNames));
        }

        public List<Process> GetProcesses()
        {
            var processes = new List<Process>();

            foreach (var name in _processNames)
            {
                try
                {
                    var found = Process.GetProcessesByName(name);
                    processes.AddRange(found);
                }
                catch (InvalidOperationException)
                {
                    // Process has exited or access is denied
                }
                catch (ArgumentException)
                {
                    // Invalid process name format
                }
            }

            return processes;
        }

        public bool IsRunning
        {
            get
            {
                RefreshState();
                return _isRunning;
            }
        }

        public event EventHandler<bool>? ProcessStateChanged;

        public void RefreshState()
        {
            var processes = GetProcesses();
            var wasRunning = _isRunning;
            _isRunning = processes.Count > 0;

            if (!_hasBeenInitialized)
            {
                _hasBeenInitialized = true;
                return;
            }

            if (wasRunning != _isRunning)
            {
                ProcessStateChanged?.Invoke(this, _isRunning);
            }
        }
    }
}
