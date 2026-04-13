using System;
using System.Diagnostics;

namespace OpenCodeSleepGuard;

public static class TaskSchedulerManager
{
    private const string TaskName = "OpenCodeSleepGuard";

    public static bool Install()
    {
        try
        {
            string exePath = GetExecutablePath();
            if (string.IsNullOrEmpty(exePath))
            {
                Console.WriteLine("Error: Could not determine the executable path.");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Create /SC ONLOGON /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /F /RL HIGHEST",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("Error: Failed to start schtasks.exe process.");
                return false;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Successfully registered task scheduler.");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to create scheduled task. Exit code: {process.ExitCode}");
                if (!string.IsNullOrWhiteSpace(error))
                    Console.WriteLine($"Error: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing scheduled task: {ex.Message}");
            return false;
        }
    }

    public static bool Uninstall()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Delete /TN \"{TaskName}\" /F",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("Error: Failed to start schtasks.exe process.");
                return false;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Successfully removed scheduled task.");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to remove scheduled task. Exit code: {process.ExitCode}");
                if (!string.IsNullOrWhiteSpace(error))
                    Console.WriteLine($"Error: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling scheduled task: {ex.Message}");
            return false;
        }
    }

    public static bool IsInstalled
    {
        get
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Query /TN \"{TaskName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return false;

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    private static string GetExecutablePath()
    {
        // Primary: Environment.ProcessPath works in most cases
        string? path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path))
            return path;

        // Fallback: Parse from command line
        string? commandLine = Environment.CommandLine;
        if (!string.IsNullOrEmpty(commandLine))
        {
            string trimmed = commandLine.Trim('"').Trim();
            int firstSpace = trimmed.IndexOf(' ');
            string candidate = firstSpace > 0 ? trimmed.Substring(0, firstSpace) : trimmed;
            candidate = candidate.Trim('"');
            if (candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return string.Empty;
    }
}