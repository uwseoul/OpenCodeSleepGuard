using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenCodeSleepGuard;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly Icon _workingIcon;
    readonly Icon _idleIcon;
    private bool _disposed;

    public event EventHandler? ExitRequested;

    public TrayIcon()
    {
        _workingIcon = CreateColoredIcon(Color.Lime, Color.White);
        _idleIcon = CreateColoredIcon(Color.Gray, Color.White);

        _contextMenu = new ContextMenuStrip();
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = "OpenCodeSleepGuard - 대기중",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };
    }

    public void SetWorking()
    {
        _notifyIcon.Icon = _workingIcon;
        _notifyIcon.Text = "OpenCodeSleepGuard - 작업중";
    }

    public void SetIdle()
    {
        _notifyIcon.Icon = _idleIcon;
        _notifyIcon.Text = "OpenCodeSleepGuard - 대기중";
    }

    private static Icon CreateColoredIcon(Color fillColor, Color borderColor)
    {
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(fillColor);
        using var pen = new Pen(borderColor, 1);
        g.FillEllipse(brush, 1, 1, 13, 13);
        g.DrawEllipse(pen, 1, 1, 13, 13);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        GC.SuppressFinalize(this);
    }

    ~TrayIcon()
    {
        Dispose();
    }
}
