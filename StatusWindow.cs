using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenCodeSleepGuard;

sealed class StatusWindow : Form
{
    private Label _lblTitle = null!;
    private Label _lblStatusTitle = null!;
    private Label _lblStatusValue = null!;
    private Label _lblProcessTitle = null!;
    private Label _lblProcessValue = null!;
    private Label _lblActivityTitle = null!;
    private Label _lblActivityValue = null!;
    private Label _lblSleepTitle = null!;
    private Label _lblSleepValue = null!;
    private Label _lblUptimeTitle = null!;
    private Label _lblUptimeValue = null!;
    private Label _lblVersion = null!;
    private Button _btnClose = null!;
    private System.Windows.Forms.Timer _uptimeTimer = null!;
    private DateTime _startTime;

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
        Size = new Size(340, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        // Title label
        _lblTitle = new Label();
        _lblTitle.Text = "🔋 OpenCodeSleepGuard";
        _lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _lblTitle.ForeColor = Color.FromArgb(40, 40, 40);
        _lblTitle.Location = new Point(16, 16);
        _lblTitle.Size = new Size(308, 30);
        _lblTitle.TextAlign = ContentAlignment.MiddleLeft;

        // Separator line
        var separator = new Label();
        separator.BackColor = Color.FromArgb(220, 220, 220);
        separator.Location = new Point(16, 52);
        separator.Size = new Size(308, 1);

        // Status row
        _lblStatusTitle = new Label();
        _lblStatusTitle.Text = "상태 (Status)";
        _lblStatusTitle.Font = new Font("Segoe UI", 9F);
        _lblStatusTitle.ForeColor = Color.FromArgb(120, 120, 120);
        _lblStatusTitle.Location = new Point(16, 68);
        _lblStatusTitle.Size = new Size(120, 20);

        _lblStatusValue = new Label();
        _lblStatusValue.Text = "⚪ 프로세스 없음";
        _lblStatusValue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblStatusValue.ForeColor = Color.FromArgb(40, 40, 40);
        _lblStatusValue.Location = new Point(136, 68);
        _lblStatusValue.Size = new Size(188, 20);

        // Process count row
        _lblProcessTitle = new Label();
        _lblProcessTitle.Text = "프로세스 (Processes)";
        _lblProcessTitle.Font = new Font("Segoe UI", 9F);
        _lblProcessTitle.ForeColor = Color.FromArgb(120, 120, 120);
        _lblProcessTitle.Location = new Point(16, 100);
        _lblProcessTitle.Size = new Size(120, 20);

        _lblProcessValue = new Label();
        _lblProcessValue.Text = "없음";
        _lblProcessValue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblProcessValue.ForeColor = Color.FromArgb(40, 40, 40);
        _lblProcessValue.Location = new Point(136, 100);
        _lblProcessValue.Size = new Size(188, 20);

        // CPU usage row
        _lblActivityTitle = new Label();
        _lblActivityTitle.Text = "감지 방식";
        _lblActivityTitle.Font = new Font("Segoe UI", 9F);
        _lblActivityTitle.ForeColor = Color.FromArgb(120, 120, 120);
        _lblActivityTitle.Location = new Point(16, 132);
        _lblActivityTitle.Size = new Size(120, 20);

        _lblActivityValue = new Label();
        _lblActivityValue.Text = "DB 폴링";
        _lblActivityValue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblActivityValue.ForeColor = Color.FromArgb(40, 40, 40);
        _lblActivityValue.Location = new Point(136, 132);
        _lblActivityValue.Size = new Size(188, 20);

        // Sleep prevention row
        _lblSleepTitle = new Label();
        _lblSleepTitle.Text = "절전 상태";
        _lblSleepTitle.Font = new Font("Segoe UI", 9F);
        _lblSleepTitle.ForeColor = Color.FromArgb(120, 120, 120);
        _lblSleepTitle.Location = new Point(16, 164);
        _lblSleepTitle.Size = new Size(120, 20);

        _lblSleepValue = new Label();
        _lblSleepValue.Text = "🔓 절전 허용";
        _lblSleepValue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSleepValue.ForeColor = Color.FromArgb(34, 139, 34);
        _lblSleepValue.Location = new Point(136, 164);
        _lblSleepValue.Size = new Size(188, 20);

        // Uptime row
        _lblUptimeTitle = new Label();
        _lblUptimeTitle.Text = "실행 시간";
        _lblUptimeTitle.Font = new Font("Segoe UI", 9F);
        _lblUptimeTitle.ForeColor = Color.FromArgb(120, 120, 120);
        _lblUptimeTitle.Location = new Point(16, 196);
        _lblUptimeTitle.Size = new Size(120, 20);

        _lblUptimeValue = new Label();
        _lblUptimeValue.Text = "0분 0초";
        _lblUptimeValue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblUptimeValue.ForeColor = Color.FromArgb(40, 40, 40);
        _lblUptimeValue.Location = new Point(136, 196);
        _lblUptimeValue.Size = new Size(188, 20);

        // Close button
        _btnClose = new Button();
        _btnClose.Text = "닫기";
        _btnClose.Font = new Font("Segoe UI", 9F);
        _btnClose.Location = new Point(136, 232);
        _btnClose.Size = new Size(80, 28);
        _btnClose.FlatStyle = FlatStyle.Flat;
        _btnClose.BackColor = Color.FromArgb(240, 240, 240);
        _btnClose.Cursor = Cursors.Hand;
        _btnClose.FlatAppearance.BorderSize = 1;
        _btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        _btnClose.Click += BtnClose_Click;

        // Version label
        _lblVersion = new Label();
        _lblVersion.Text = "v1.2.0";
        _lblVersion.Font = new Font("Segoe UI", 8F);
        _lblVersion.ForeColor = Color.FromArgb(150, 150, 150);
        _lblVersion.Location = new Point(16, 270);
        _lblVersion.Size = new Size(60, 16);
        _lblVersion.TextAlign = ContentAlignment.MiddleLeft;

        // Add controls
        Controls.Add(_lblTitle);
        Controls.Add(separator);
        Controls.Add(_lblStatusTitle);
        Controls.Add(_lblStatusValue);
        Controls.Add(_lblProcessTitle);
        Controls.Add(_lblProcessValue);
        Controls.Add(_lblActivityTitle);
        Controls.Add(_lblActivityValue);
        Controls.Add(_lblSleepTitle);
        Controls.Add(_lblSleepValue);
        Controls.Add(_lblUptimeTitle);
        Controls.Add(_lblUptimeValue);
        Controls.Add(_btnClose);
        Controls.Add(_lblVersion);
    }

    public void UpdateStatus(bool isRunning, int processCount, bool isWorking, bool isSleepPrevented)
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => UpdateStatus(isRunning, processCount, isWorking, isSleepPrevented))); }
            catch (InvalidOperationException) { }
            return;
        }

        // Status
        if (!isRunning || processCount == 0)
        {
            _lblStatusValue.Text = "⚪ 프로세스 없음";
            _lblStatusValue.ForeColor = Color.FromArgb(40, 40, 40);
        }
        else if (isWorking)
        {
            _lblStatusValue.Text = "🟢 작업 중";
            _lblStatusValue.ForeColor = Color.FromArgb(34, 139, 34);
        }
        else
        {
            _lblStatusValue.Text = "⚪ 대기 중";
            _lblStatusValue.ForeColor = Color.FromArgb(40, 40, 40);
        }

        // Process count
        _lblProcessValue.Text = processCount > 0 ? $"{processCount}개 감지됨" : "없음";

        _lblActivityValue.Text = isWorking ? "DB 폴링 (작업 감지)" : "DB 폴링 (대기 감지)";
        _lblActivityValue.ForeColor = isWorking
            ? Color.FromArgb(34, 139, 34)
            : Color.FromArgb(120, 120, 120);

        // Sleep prevention state
        if (isSleepPrevented)
        {
            _lblSleepValue.Text = "🔒 절전 방지 중";
            _lblSleepValue.ForeColor = Color.FromArgb(200, 80, 20);
        }
        else
        {
            _lblSleepValue.Text = "🔓 절전 허용";
            _lblSleepValue.ForeColor = Color.FromArgb(34, 139, 34);
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
    }

    private void BtnClose_Click(object? sender, EventArgs e)
    {
        Hide();
        WindowClosed?.Invoke(this, EventArgs.Empty);
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
