using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenCodeSleepGuard;

sealed class StatusWindow : Form
{
    // ── Language ──
    private enum Lang { EN, KO }
    private static Lang _lang = Lang.EN;

    // ── Localized strings ──
    private static class L
    {
        // Labels
        public static string StatusTitle       => _lang == Lang.EN ? "Status"          : "상태";
        public static string SessionTitle      => _lang == Lang.EN ? "Session"         : "세션 정보";
        public static string AgentTitle        => _lang == Lang.EN ? "Agent"           : "에이전트 정보";
        public static string TaskTitle         => _lang == Lang.EN ? "Task"            : "작업 정보";
        public static string EventTitle        => _lang == Lang.EN ? "Recent Event"    : "최근 이벤트";
        public static string LastActivityTitle => _lang == Lang.EN ? "Last Activity"   : "마지막 활동";
        public static string SleepTitle        => _lang == Lang.EN ? "Sleep State"     : "절전 상태";
        public static string UptimeTitle       => _lang == Lang.EN ? "Uptime"          : "실행 시간";
        public static string Close             => _lang == Lang.EN ? "Close"           : "닫기";
        public static string WindowTitle       => _lang == Lang.EN ? "OpenCodeSleepGuard" : "OpenCodeSleepGuard";

        // Status values
        public static string NoProcess     => _lang == Lang.EN ? "⚪ No Process"      : "⚪ 프로세스 없음";
        public static string Working       => _lang == Lang.EN ? "🟢 Working"         : "🟢 작업 중";
        public static string Idle          => _lang == Lang.EN ? "⚪ Idle"            : "⚪ 대기 중";
        public static string SleepPrevent  => _lang == Lang.EN ? "🔒 Preventing Sleep" : "🔒 절전 방지 중";
        public static string SleepAllow    => _lang == Lang.EN ? "🔓 Sleep Allowed"   : "🔓 절전 허용";
        public static string None          => _lang == Lang.EN ? "None"               : "없음";

        // Uptime
        public static string UptimeSeconds(int s) => _lang == Lang.EN ? $"{s}s"                     : $"{s}초";
        public static string UptimeMinutes(int m, int s) => _lang == Lang.EN ? $"{m}m {s}s"          : $"{m}분 {s}초";
        public static string UptimeHours(int h, int m, int s) => _lang == Lang.EN ? $"{h}h {m}m {s}s" : $"{h}시간 {m}분 {s}초";

        // Relative time
        public static string SecondsAgo(int s) => _lang == Lang.EN ? $"{s}s ago"   : $"{s}초 전";
        public static string MinutesAgo(int m) => _lang == Lang.EN ? $"{m}m ago"   : $"{m}분 전";
        public static string HoursAgo(int h)   => _lang == Lang.EN ? $"{h}h ago"   : $"{h}시간 전";

        // Initial uptime
        public static string UptimeZero => _lang == Lang.EN ? "0s" : "0초";
    }

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
    private static readonly Color CLangActive = Color.FromArgb(79, 106, 255);
    private static readonly Color CLangInactive = Color.FromArgb(80, 84, 120);

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
    private Button _btnEN = null!;
    private Button _btnKO = null!;
    private System.Windows.Forms.Timer _uptimeTimer = null!;
    private DateTime _startTime;
    private DateTime? _lastActivityValueTimestamp;

    // Cached state for language refresh
    private bool _cachedIsRunning;
    private bool _cachedIsWorking;
    private string _cachedLastActivity = "";
    private string _cachedSessionTitle = "";
    private string _cachedAgentName = "";
    private string _cachedTaskInfo = "";
    private string _cachedDbStatus = "";
    private bool _cachedIsSleepPrevented;

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
        Text = L.WindowTitle;
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
            Size = new Size(310, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // ═══ Language Buttons (top-right) ═══
        _btnEN = new Button
        {
            Text = "EN",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Size = new Size(36, 24),
            Location = new Point(348, 16),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = CLangActive,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _btnEN.FlatAppearance.BorderSize = 0;
        _btnEN.FlatAppearance.MouseOverBackColor = CLangActive;
        _btnEN.FlatAppearance.MouseDownBackColor = CLangActive;
        _btnEN.Click += (s, e) => SetLanguage(Lang.EN);

        _btnKO = new Button
        {
            Text = "KO",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Size = new Size(36, 24),
            Location = new Point(388, 16),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = CLangInactive,
            ForeColor = Color.FromArgb(160, 163, 190),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _btnKO.FlatAppearance.BorderSize = 0;
        _btnKO.FlatAppearance.MouseOverBackColor = CLangInactive;
        _btnKO.FlatAppearance.MouseDownBackColor = CLangInactive;
        _btnKO.Click += (s, e) => SetLanguage(Lang.KO);

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

        _lblStatusTitle = MakeLabel(L.StatusTitle, 10);
        _lblStatusValue = new Label
        {
            Text = L.NoProcess,
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

        _lblSleepTitle = MakeLabel(L.SleepTitle, 46);
        _lblSleepValue = new Label
        {
            Text = L.SleepAllow,
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

        _lblSessionTitle       = MakeLabel(L.SessionTitle, 10);
        _lblSessionValue       = MakeValue("-", 10);
        _lblAgentTitle         = MakeLabel(L.AgentTitle, 40);
        _lblAgentValue         = MakeValue("-", 40);
        _lblTaskTitle          = MakeLabel(L.TaskTitle, 70);
        _lblTaskValue          = MakeValue("-", 70);
        _lblEventTitle         = MakeLabel(L.EventTitle, 100);
        _lblEventValue         = MakeValue(L.None, 100);
        _lblLastActivityTitle  = MakeLabel(L.LastActivityTitle, 130);
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
            Text = L.UptimeTitle,
            Font = new Font("Segoe UI", 9F),
            ForeColor = CLabel,
            BackColor = Color.Transparent,
            Location = new Point(20, 314),
            Size = new Size(100, 20)
        };

        _lblUptimeValue = new Label
        {
            Text = L.UptimeZero,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = CValue,
            BackColor = Color.Transparent,
            Location = new Point(120, 314),
            Size = new Size(310, 20)
        };

        // ═══ Close Button ═══
        _btnClose = new Button
        {
            Text = L.Close,
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
            _lblTitle, _btnEN, _btnKO, accentLine,
            _cardPrimary, _cardDetails,
            _lblUptimeTitle, _lblUptimeValue,
            _btnClose, _lblVersion
        });
    }

    private void SetLanguage(Lang lang)
    {
        _lang = lang;

        // Update language button visuals
        _btnEN.BackColor = lang == Lang.EN ? CLangActive : CLangInactive;
        _btnEN.ForeColor = lang == Lang.EN ? Color.White : Color.FromArgb(160, 163, 190);
        _btnEN.FlatAppearance.MouseOverBackColor = _btnEN.BackColor;
        _btnEN.FlatAppearance.MouseDownBackColor = _btnEN.BackColor;

        _btnKO.BackColor = lang == Lang.KO ? CLangActive : CLangInactive;
        _btnKO.ForeColor = lang == Lang.KO ? Color.White : Color.FromArgb(160, 163, 190);
        _btnKO.FlatAppearance.MouseOverBackColor = _btnKO.BackColor;
        _btnKO.FlatAppearance.MouseDownBackColor = _btnKO.BackColor;

        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = L.WindowTitle;

        // Static labels
        _lblStatusTitle.Text       = L.StatusTitle;
        _lblSleepTitle.Text        = L.SleepTitle;
        _lblSessionTitle.Text      = L.SessionTitle;
        _lblAgentTitle.Text        = L.AgentTitle;
        _lblTaskTitle.Text         = L.TaskTitle;
        _lblEventTitle.Text        = L.EventTitle;
        _lblLastActivityTitle.Text = L.LastActivityTitle;
        _lblUptimeTitle.Text       = L.UptimeTitle;
        _btnClose.Text             = L.Close;

        // Dynamic values — replay cached state
        ApplyStatusValues();
    }

    private void ApplyStatusValues()
    {
        // Status
        if (!_cachedIsRunning)
        {
            _lblStatusValue.Text = L.NoProcess;
            _lblStatusValue.ForeColor = CDimValue;
        }
        else if (_cachedIsWorking)
        {
            _lblStatusValue.Text = L.Working;
            _lblStatusValue.ForeColor = CGreen;
        }
        else
        {
            _lblStatusValue.Text = L.Idle;
            _lblStatusValue.ForeColor = CValue;
        }

        // Event
        _lblEventValue.Text = string.IsNullOrWhiteSpace(_cachedLastActivity) ? L.None : _cachedLastActivity;
        _lblEventValue.ForeColor = _cachedIsWorking ? CGreen : CValue;

        // Session / Agent / Task
        _lblSessionValue.Text = string.IsNullOrWhiteSpace(_cachedSessionTitle) ? "-" : _cachedSessionTitle;
        _lblAgentValue.Text = string.IsNullOrWhiteSpace(_cachedAgentName) ? "-" : _cachedAgentName;
        _lblTaskValue.Text = string.IsNullOrWhiteSpace(_cachedTaskInfo) ? _cachedDbStatus : $"{_cachedTaskInfo} ({_cachedDbStatus})";

        // Last activity
        _lblLastActivityValue.Text = _lastActivityValueTimestamp.HasValue
            ? FormatRelativeTime(_lastActivityValueTimestamp.Value)
            : "-";
        _lblLastActivityValue.ForeColor = CValue;

        // Sleep prevention state
        if (_cachedIsSleepPrevented)
        {
            _lblSleepValue.Text = L.SleepPrevent;
            _lblSleepValue.ForeColor = CAmber;
        }
        else
        {
            _lblSleepValue.Text = L.SleepAllow;
            _lblSleepValue.ForeColor = CGreen;
        }

        // Uptime
        var elapsed = DateTime.UtcNow - _startTime;
        _lblUptimeValue.Text = FormatUptime(elapsed);
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

        // Cache current state
        _cachedIsRunning = isRunning;
        _cachedIsWorking = isWorking;
        _cachedLastActivity = lastActivity;
        _cachedSessionTitle = sessionTitle;
        _cachedAgentName = agentName;
        _cachedTaskInfo = taskInfo;
        _cachedDbStatus = dbStatus;
        _cachedIsSleepPrevented = isSleepPrevented;
        _lastActivityValueTimestamp = lastActivityTime;

        ApplyStatusValues();
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
        _lblUptimeValue.Text = FormatUptime(elapsed);

        if (_lastActivityValueTimestamp.HasValue)
        {
            _lblLastActivityValue.Text = FormatRelativeTime(_lastActivityValueTimestamp.Value);
        }
    }

    private static string FormatUptime(TimeSpan elapsed)
    {
        var hours = (int)elapsed.TotalHours;
        if (hours >= 1)
            return L.UptimeHours(hours, elapsed.Minutes, elapsed.Seconds);
        if (elapsed.Minutes >= 1)
            return L.UptimeMinutes(elapsed.Minutes, elapsed.Seconds);
        return L.UptimeSeconds(elapsed.Seconds);
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
            delta = TimeSpan.Zero;

        if (delta.TotalMinutes < 1)
            return L.SecondsAgo(Math.Max(0, (int)delta.TotalSeconds));

        if (delta.TotalHours < 1)
            return L.MinutesAgo((int)delta.TotalMinutes);

        return L.HoursAgo((int)delta.TotalHours);
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
