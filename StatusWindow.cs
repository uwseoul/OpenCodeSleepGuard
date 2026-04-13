using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenCodeSleepGuard;

sealed class StatusWindow : Form
{
    // ── Dark theme palette ──
    private static readonly Color CBackground = Color.FromArgb(27, 29, 47);
    private static readonly Color CCard       = Color.FromArgb(36, 39, 62);
    private static readonly Color CAccent     = Color.FromArgb(79, 106, 255);
    private static readonly Color CTitle      = Color.FromArgb(235, 237, 255);
    private static readonly Color CLabel      = Color.FromArgb(120, 124, 155);
    private static readonly Color CValue      = Color.FromArgb(200, 204, 230);
    private static readonly Color CDimValue   = Color.FromArgb(120, 124, 155);
    private static readonly Color CGreen      = Color.FromArgb(74, 222, 128);
    private static readonly Color CAmber      = Color.FromArgb(251, 191, 36);
    private static readonly Color CSeparator  = Color.FromArgb(50, 53, 85);
    private static readonly Color CBtnBg      = Color.FromArgb(48, 51, 85);
    private static readonly Color CBtnHover   = Color.FromArgb(60, 64, 105);
    private static readonly Color CBtnPress   = Color.FromArgb(72, 76, 125);
    private static readonly Color CBtnBorder  = Color.FromArgb(66, 70, 115);
    private static readonly Color CBtnText    = Color.FromArgb(195, 198, 222);

    private Panel _cardPrimary = null!;
    private Panel _cardDetails = null!;
    private Label _lblTitle = null!;
    private Label _lblStatusTitle = null!;
    private Label _lblStatusValue = null!;
    private Label _lblSessionTitle = null!;
    private Label _lblSessionValue = null!;
    private Label _lblAgentTitle = null!;
    private Label _lblAgentValue = null!;
    private Label _lblTaskTitle = null!;
    private Label _lblTaskValue = null!;
    private Label _lblEventTitle = null!;
    private Label _lblEventValue = null!;
    private Label _lblLastActivityTitle = null!;
    private Label _lblLastActivityValue = null!;
    private Label _lblSleepTitle = null!;
    private Label _lblSleepValue = null!;
    private Label _lblUptimeTitle = null!;
    private Label _lblUptimeValue = null!;
    private Label _lblVersion = null!;
    private Button _btnClose = null!;
    private System.Windows.Forms.Timer _uptimeTimer = null!;
    private DateTime _startTime;
    private DateTime? _lastActivityValueTimestamp;

    public event EventHandler? WindowClosed = null;

    public StatusWindow()
    {
        _startTime = DateTime.UtcNow;
        InitializeComponent();

        _uptimeTimer = new System.Windows.Forms.Timer();
        _uptimeTimer.Interval = 1000;
        _uptimeTimer.Tick += UptimeTimer_Tick;
    }

    private void InitializeComponent()
    {
        Text = "OpenCodeSleepGuard";
        Size = new Size(450, 478);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = CBackground;
        Font = new Font("Segoe UI", 9F);

        // ═══ Title ═══
        _lblTitle = new Label
        {
            Text = "🔋 OpenCodeSleepGuard",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = CTitle,
            BackColor = Color.Transparent,
            Location = new Point(20, 14),
            Size = new Size(410, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Accent line under title
        var accentLine = new Label
        {
            BackColor = CAccent,
            Location = new Point(20, 48),
            Size = new Size(410, 2)
        };

        // ═══ Primary Status Card ═══
        _cardPrimary = new Panel
        {
            BackColor = CCard,
            Location = new Point(16, 58),
            Size = new Size(418, 76)
        };

        _lblStatusTitle = MakeLabel("상태", 10);
        _lblStatusValue = new Label
        {
            Text = "⚪ 프로세스 없음",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = CDimValue,
            BackColor = Color.Transparent,
            Location = new Point(118, 8),
            Size = new Size(286, 24)
        };

        var cardSeparator = new Label
        {
            BackColor = CSeparator,
            Location = new Point(14, 38),
            Size = new Size(390, 1)
        };

        _lblSleepTitle = MakeLabel("절전 상태", 46);
        _lblSleepValue = new Label
        {
            Text = "🔓 절전 허용",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = CGreen,
            BackColor = Color.Transparent,
            Location = new Point(118, 44),
            Size = new Size(286, 24)
        };

        _cardPrimary.Controls.AddRange(new Control[]
        {
            _lblStatusTitle, _lblStatusValue, cardSeparator,
            _lblSleepTitle, _lblSleepValue
        });

        // ═══ Details Card ═══
        _cardDetails = new Panel
        {
            BackColor = CCard,
            Location = new Point(16, 142),
            Size = new Size(418, 160)
        };

        _lblSessionTitle       = MakeLabel("세션 정보", 10);
        _lblSessionValue       = MakeValue("-", 10);
        _lblAgentTitle         = MakeLabel("에이전트 정보", 40);
        _lblAgentValue         = MakeValue("-", 40);
        _lblTaskTitle          = MakeLabel("작업 정보", 70);
        _lblTaskValue          = MakeValue("-", 70);
        _lblEventTitle         = MakeLabel("최근 이벤트", 100);
        _lblEventValue         = MakeValue("없음", 100);
        _lblLastActivityTitle  = MakeLabel("마지막 활동", 130);
        _lblLastActivityValue  = MakeValue("-", 130);

        _cardDetails.Controls.AddRange(new Control[]
        {
            _lblSessionTitle, _lblSessionValue,
            _lblAgentTitle, _lblAgentValue,
            _lblTaskTitle, _lblTaskValue,
            _lblEventTitle, _lblEventValue,
            _lblLastActivityTitle, _lblLastActivityValue
        });

        // ═══ Uptime ═══
        _lblUptimeTitle = new Label
        {
            Text = "실행 시간",
            Font = new Font("Segoe UI", 9F),
            ForeColor = CLabel,
            BackColor = Color.Transparent,
            Location = new Point(20, 314),
            Size = new Size(100, 20)
        };

        _lblUptimeValue = new Label
        {
            Text = "0초",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = CValue,
            BackColor = Color.Transparent,
            Location = new Point(120, 314),
            Size = new Size(310, 20)
        };

        // ═══ Close Button ═══
        _btnClose = new Button
        {
            Text = "닫기",
            Font = new Font("Segoe UI", 9F),
            BackColor = CBtnBg,
            ForeColor = CBtnText,
            Location = new Point(175, 350),
            Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnClose.FlatAppearance.BorderSize = 1;
        _btnClose.FlatAppearance.BorderColor = CBtnBorder;
        _btnClose.FlatAppearance.MouseOverBackColor = CBtnHover;
        _btnClose.FlatAppearance.MouseDownBackColor = CBtnPress;
        _btnClose.Click += BtnClose_Click;

        // ═══ Version ═══
        _lblVersion = new Label
        {
            Text = "v1.2.0",
            Font = new Font("Segoe UI", 8F),
            ForeColor = CLabel,
            BackColor = Color.Transparent,
            Location = new Point(20, 394),
            Size = new Size(60, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // ═══ Add Controls ═══
        Controls.AddRange(new Control[]
        {
            _lblTitle, accentLine,
            _cardPrimary, _cardDetails,
            _lblUptimeTitle, _lblUptimeValue,
            _btnClose, _lblVersion
        });
    }

    private static Label MakeLabel(string text, int y)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9F),
            ForeColor = CLabel,
            BackColor = Color.Transparent,
            Location = new Point(14, y),
            Size = new Size(100, 20)
        };
    }

    private static Label MakeValue(string text, int y)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = CValue,
            BackColor = Color.Transparent,
            Location = new Point(118, y),
            Size = new Size(286, 20)
        };
    }

    public void UpdateStatus(bool isRunning, bool isWorking, string lastActivity, DateTime? lastActivityTime, string sessionTitle, string agentName, string taskInfo, string dbStatus, bool isSleepPrevented)
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => UpdateStatus(isRunning, isWorking, lastActivity, lastActivityTime, sessionTitle, agentName, taskInfo, dbStatus, isSleepPrevented))); }
            catch (InvalidOperationException) { }
            return;
        }

        // Status
        if (!isRunning)
        {
            _lblStatusValue.Text = "⚪ 프로세스 없음";
            _lblStatusValue.ForeColor = CDimValue;
        }
        else if (isWorking)
        {
            _lblStatusValue.Text = "🟢 작업 중";
            _lblStatusValue.ForeColor = CGreen;
        }
        else
        {
            _lblStatusValue.Text = "⚪ 대기 중";
            _lblStatusValue.ForeColor = CValue;
        }

        _lblEventValue.Text = string.IsNullOrWhiteSpace(lastActivity) ? "없음" : lastActivity;
        _lblEventValue.ForeColor = isWorking ? CGreen : CValue;

        _lblSessionValue.Text = string.IsNullOrWhiteSpace(sessionTitle) ? "-" : sessionTitle;
        _lblAgentValue.Text = string.IsNullOrWhiteSpace(agentName) ? "-" : agentName;
        _lblTaskValue.Text = string.IsNullOrWhiteSpace(taskInfo) ? dbStatus : $"{taskInfo} ({dbStatus})";

        _lastActivityValueTimestamp = lastActivityTime;
        _lblLastActivityValue.Text = lastActivityTime.HasValue
            ? FormatRelativeTime(lastActivityTime.Value)
            : "-";
        _lblLastActivityValue.ForeColor = CValue;

        // Sleep prevention state
        if (isSleepPrevented)
        {
            _lblSleepValue.Text = "🔒 절전 방지 중";
            _lblSleepValue.ForeColor = CAmber;
        }
        else
        {
            _lblSleepValue.Text = "🔓 절전 허용";
            _lblSleepValue.ForeColor = CGreen;
        }
    }

    public void ShowStatus()
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(ShowStatus)); }
            catch (InvalidOperationException) { }
            return;
        }
        base.Show();
        Activate();
        _uptimeTimer.Start();
    }

    public new void Hide()
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(Hide)); }
            catch (InvalidOperationException) { }
            return;
        }
        _uptimeTimer.Stop();
        base.Hide();
    }

    private void UptimeTimer_Tick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.UtcNow - _startTime;
        var hours = (int)elapsed.TotalHours;
        if (hours >= 1)
            _lblUptimeValue.Text = $"{hours}시간 {elapsed.Minutes}분 {elapsed.Seconds}초";
        else if (elapsed.Minutes >= 1)
            _lblUptimeValue.Text = $"{elapsed.Minutes}분 {elapsed.Seconds}초";
        else
            _lblUptimeValue.Text = $"{elapsed.Seconds}초";

        if (_lastActivityValueTimestamp.HasValue)
        {
            _lblLastActivityValue.Text = FormatRelativeTime(_lastActivityValueTimestamp.Value);
        }
    }

    private void BtnClose_Click(object? sender, EventArgs e)
    {
        Hide();
        WindowClosed?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatRelativeTime(DateTime activityTime)
    {
        var delta = DateTime.Now - activityTime.ToLocalTime();
        if (delta.TotalSeconds < 0)
        {
            delta = TimeSpan.Zero;
        }

        if (delta.TotalMinutes < 1)
        {
            return $"{Math.Max(0, (int)delta.TotalSeconds)}초 전";
        }

        if (delta.TotalHours < 1)
        {
            return $"{(int)delta.TotalMinutes}분 전";
        }

        return $"{(int)delta.TotalHours}시간 전";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _uptimeTimer?.Stop();
            _uptimeTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
