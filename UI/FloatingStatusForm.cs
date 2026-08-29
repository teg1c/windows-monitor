namespace MhxyNotify.UI;

public sealed class FloatingStatusForm : Form
{
    private static readonly Color SurfaceBack = Color.FromArgb(16, 44, 37);

    private readonly AntdUI.Panel _surface = new();
    private readonly TableLayoutPanel _layout = new();
    private readonly Label _stateLabel = new();
    private readonly Label _indicatorLabel = new();
    private readonly Label _messageLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Label _timeLabel = new();
    private readonly ContextMenuStrip _contextMenu = new();
    private readonly ToolStripMenuItem _toggleMonitorItem = new();
    private bool _monitoring;
    private Point _dragStart;

    public FloatingStatusForm()
    {
        Text = "\u7075\u8baf\u54e8 \u72b6\u6001\u6d6e\u7a97";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        Size = new Size(320, 108);
        MinimumSize = Size;
        MaximumSize = Size;
        BackColor = SurfaceBack;
        Opacity = 0.98;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Padding = Padding.Empty;

        _surface.Dock = DockStyle.Fill;
        _surface.BackColor = SurfaceBack;
        _surface.Back = SurfaceBack;
        _surface.Radius = 16;
        _surface.BorderWidth = 1;
        _surface.BorderColor = Color.FromArgb(58, 91, 80);
        _surface.Padding = new Padding(16, 10, 16, 10);

        _layout.Dock = DockStyle.Fill;
        _layout.BackColor = Color.Transparent;
        _layout.ColumnCount = 2;
        _layout.RowCount = 3;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

        _stateLabel.Dock = DockStyle.Fill;
        _stateLabel.ForeColor = Color.White;
        _stateLabel.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold, GraphicsUnit.Point);
        _stateLabel.TextAlign = ContentAlignment.MiddleLeft;

        _indicatorLabel.Dock = DockStyle.Fill;
        _indicatorLabel.ForeColor = Color.FromArgb(126, 160, 148);
        _indicatorLabel.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point);
        _indicatorLabel.Text = "\u25cf";
        _indicatorLabel.TextAlign = ContentAlignment.MiddleRight;

        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.ForeColor = Color.FromArgb(205, 225, 218);
        _messageLabel.AutoEllipsis = true;
        _messageLabel.TextAlign = ContentAlignment.MiddleLeft;

        _hintLabel.Dock = DockStyle.Fill;
        _hintLabel.ForeColor = Color.FromArgb(107, 150, 134);
        _hintLabel.Font = new Font(Font.FontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);
        _hintLabel.Text = "\u53cc\u51fb\u6253\u5f00";
        _hintLabel.TextAlign = ContentAlignment.MiddleLeft;

        _timeLabel.Dock = DockStyle.Fill;
        _timeLabel.ForeColor = Color.FromArgb(137, 176, 162);
        _timeLabel.Font = new Font(Font.FontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);
        _timeLabel.TextAlign = ContentAlignment.MiddleRight;

        _layout.Controls.Add(_stateLabel, 0, 0);
        _layout.Controls.Add(_indicatorLabel, 1, 0);
        _layout.Controls.Add(_messageLabel, 0, 1);
        _layout.SetColumnSpan(_messageLabel, 2);
        _layout.Controls.Add(_hintLabel, 0, 2);
        _layout.Controls.Add(_timeLabel, 1, 2);
        _surface.Controls.Add(_layout);
        Controls.Add(_surface);

        _toggleMonitorItem.Click += (_, _) => ToggleMonitorRequested?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(_toggleMonitorItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("\u9690\u85cf\u6d6e\u7a97", null, (_, _) => HideRequested?.Invoke(this, EventArgs.Empty));
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("\u9000\u51fa\u8f6f\u4ef6", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        _contextMenu.Opening += (_, _) => UpdateMenuText();
        ContextMenuStrip = _contextMenu;

        foreach (Control control in new Control[] { this, _surface, _layout, _stateLabel, _indicatorLabel, _messageLabel, _hintLabel, _timeLabel })
        {
            control.ContextMenuStrip = _contextMenu;
            control.MouseDown += BeginDrag;
            control.MouseMove += DragMove;
            control.DoubleClick += (_, _) => RestoreRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? RestoreRequested;
    public event EventHandler? ToggleMonitorRequested;
    public event EventHandler? HideRequested;
    public event EventHandler? ExitRequested;

    public void UpdateStatus(string state, string message, bool monitoring, bool previewing)
    {
        _monitoring = monitoring;
        _stateLabel.Text = state;
        _messageLabel.Text = string.IsNullOrWhiteSpace(message) ? "\u5c31\u7eea" : message;
        _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        _indicatorLabel.ForeColor = monitoring
            ? Color.FromArgb(68, 211, 153)
            : previewing
                ? Color.FromArgb(91, 168, 218)
                : Color.FromArgb(126, 160, 148);
        UpdateMenuText();
        Invalidate(true);
    }

    private void UpdateMenuText()
    {
        _toggleMonitorItem.Text = _monitoring ? "\u505c\u6b62\u76d1\u63a7" : "\u5f00\u59cb\u76d1\u63a7";
    }

    public void MoveToDefaultLocation()
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(screen.Right - Width - 18, screen.Top + 18);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = CreateRoundPath(ClientRectangle, 16);
        Region?.Dispose();
        Region = new Region(path);
    }

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragStart = e.Location;
        }
    }

    private void DragMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var screen = PointToScreen(e.Location);
        Location = new Point(screen.X - _dragStart.X, screen.Y - _dragStart.Y);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundPath(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
