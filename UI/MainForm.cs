using MhxyNotify.Models;
using MhxyNotify.Native;
using MhxyNotify.Services;

namespace MhxyNotify.UI;

public sealed class MainForm : Form
{
    private const int SettingsPanelWidth = 560;
    private static readonly Color PageBack = Color.FromArgb(245, 247, 250);
    private static readonly Color PanelBack = Color.White;
    private static readonly Color HeaderBack = Color.FromArgb(26, 36, 48);
    private static readonly Color Accent = Color.FromArgb(20, 132, 92);
    private static readonly Color Danger = Color.FromArgb(204, 74, 74);
    private static readonly Color MutedText = Color.FromArgb(90, 99, 112);

    private readonly AppConfig _config;
    private readonly LogService _logService = new();
    private readonly NotificationService _notificationService;
    private readonly WxOcrService _wxOcrService = new();
    private readonly CommandOcrService _commandOcrService = new();
    private readonly LocalPaddleOcrService _localOcrService = new();
    private readonly UpdateService _updateService;
    private readonly System.Windows.Forms.Timer _timer = new();

    private readonly ComboBox _windowList = new();
    private readonly ComboBox _taskbarWindowList = new();
    private readonly Button _refreshButton = new();
    private readonly Button _refreshTaskbarWindowsButton = new();
    private readonly Button _previewButton = new();
    private readonly Button _monitorButton = new();
    private readonly PreviewCanvas _preview = new();
    private readonly CheckBox _ocrKeywordEnabled = new();
    private readonly CheckBox _dialogKeywordEnabled = new();
    private readonly CheckBox _taskbarFlashEnabled = new();
    private readonly NumericUpDown _intervalInput = new();
    private readonly NumericUpDown _cooldownInput = new();
    private readonly NumericUpDown _maxUnchangedNotifyInput = new();

    private readonly CheckBox _ocrEnabled = new();
    private readonly ComboBox _ocrMode = new();
    private readonly TextBox _ocrUrl = new();
    private readonly TextBox _ocrCommand = new();
    private readonly TextBox _ocrArguments = new();
    private readonly NumericUpDown _minConfidenceInput = new();
    private readonly NumericUpDown _maxOcrPixelsInput = new();
    private readonly TextBox _ocrText = new();
    private readonly NumericUpDown _keywordOcrIntervalInput = new();
    private readonly NumericUpDown _dialogIntervalInput = new();
    private readonly TextBox _watchKeywords = new();
    private readonly TextBox _dialogKeywords = new();
    private readonly TextBox _taskbarFlashKeywords = new();
    private readonly TextBox _keywordLastHitText = new();
    private readonly Label _taskbarFlashStatus = new();

    private readonly CheckBox _webhookEnabled = new();
    private readonly CheckedListBox _webhookChannels = new();
    private readonly TextBox _webhookName = new();
    private readonly ComboBox _webhookPreset = new();
    private readonly Button _applyWebhookPresetButton = new();
    private readonly Button _addWebhookChannelButton = new();
    private readonly Button _saveWebhookChannelButton = new();
    private readonly Button _deleteWebhookChannelButton = new();
    private readonly TextBox _webhookMethod = new();
    private readonly TextBox _webhookUrl = new();
    private readonly TextBox _webhookHeaders = new();
    private readonly TextBox _webhookBodyTemplate = new();
    private readonly TextBox _webhookOcrBodyTemplate = new();
    private readonly TextBox _webhookDialogBodyTemplate = new();
    private readonly TextBox _webhookTaskbarBodyTemplate = new();
    private readonly TabControl _webhookBodyTabs = new();
    private readonly CheckBox _windowsVoiceEnabled = new();
    private readonly TextBox _windowsOcrVoiceTemplate = new();
    private readonly TextBox _windowsDialogVoiceTemplate = new();
    private readonly TextBox _windowsTaskbarVoiceTemplate = new();
    private readonly TabControl _windowsVoiceTabs = new();
    private readonly CheckBox _webhookForOcr = new();
    private readonly CheckBox _webhookForDialog = new();
    private readonly CheckBox _webhookForTaskbar = new();
    private readonly List<NotificationChannelConfig> _notificationChannels = [];
    private readonly List<Control> _webhookOnlyControls = [];
    private readonly List<Control> _windowsOnlyControls = [];

    private readonly Label _regionLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _statePill = new();
    private readonly Button _checkUpdateButton = new();
    private readonly TextBox _logViewer = new();
    private readonly NumericUpDown _maxLogLinesInput = new();
    private readonly Button _refreshLogButton = new();
    private readonly Button _clearLogButton = new();
    private readonly System.Windows.Forms.Panel _pageHost = new();
    private readonly Panel _previewShell = new();
    private readonly Label _previewEmptyLabel = new();
    private FlowLayoutPanel? _monitorTasksHost;
    private readonly Panel _monitorSubPageHost = new();
    private readonly List<Control> _monitorSubPages = [];
    private readonly CenteredTabLabel _monitorOcrTaskButton = new();
    private readonly CenteredTabLabel _monitorDialogTaskButton = new();
    private readonly CenteredTabLabel _monitorTaskbarTaskButton = new();
    private TableLayoutPanel? _monitorOcrLayout;
    private Control? _monitorPreviewArea;
    private readonly List<Control> _settingPages = [];
    private readonly List<Control> _sourceSelectionControls = [];
    private readonly List<Control> _regionSelectionControls = [];
    private readonly List<Control> _ocrSourceControls = [];
    private readonly CenteredTabLabel _navMonitorButton = new();
    private readonly CenteredTabLabel _navOcrButton = new();
    private readonly CenteredTabLabel _navWebhookButton = new();
    private readonly CenteredTabLabel _navLogButton = new();

    private bool _previewing;
    private bool _monitoring;
    private bool _polling;
    private bool _allowClose;
    private bool _closing;
    private bool _loadingConfig;
    private bool _updatingWebhookChannels;
    private uint _shellHookMessage;
    private bool _shellHookRegistered;
    private IntPtr _alertWinEventHook;
    private readonly Win32Window.WinEventDelegate _alertWinEventCallback;
    private WindowInfo? _selectedWindow;
    private DateTimeOffset _lastKeywordOcrAt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastDialogKeywordAt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastTaskbarFlashEventAt = DateTimeOffset.MinValue;
    private IntPtr _lastTaskbarFlashEventHandle;
    private readonly AlertState _ocrAlertState = new();
    private readonly AlertState _dialogAlertState = new();
    private readonly AlertState _taskbarAlertState = new();

    private sealed class AlertState
    {
        public string LastHit { get; set; } = "";
        public string LastSignature { get; set; } = "";
        public int UnchangedNotifyCount { get; set; }
        public DateTimeOffset LastNotifyAt { get; set; } = DateTimeOffset.MinValue;

        public void Reset()
        {
            LastHit = "";
            LastSignature = "";
            UnchangedNotifyCount = 0;
            LastNotifyAt = DateTimeOffset.MinValue;
        }
    }

    private sealed class CenteredTabLabel : Label
    {
        public CenteredTabLabel()
        {
            AutoSize = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var background = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(background, ClientRectangle);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var brush = new SolidBrush(ForeColor);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            var bounds = ClientRectangle;
            bounds.Inflate(0, -1);
            e.Graphics.DrawString(Text, Font, brush, bounds, format);
        }
    }

    public MainForm()
    {
        _config = AppConfig.Load();
        _notificationService = new NotificationService(_logService);
        _updateService = new UpdateService(_logService);
        _alertWinEventCallback = OnAlertWinEvent;
        Text = AppInfo.FullTitle;
        Icon = AppInfo.LoadApplicationIcon();
        MinimumSize = new Size(1180, 780);
        Size = new Size(1320, 860);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = PageBack;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        BuildUi();
        LoadConfigIntoUi();
        RefreshWindows();
        RefreshLogViewer();

        _timer.Tick += TimerOnTick;
        Shown += async (_, _) => await CheckForUpdatesAsync(userInitiated: false);
        UpdateActionButtons();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = PageBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildSettingsTabs(), 0, 1);
        root.Controls.Add(BuildStatusBar(), 0, 2);
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = HeaderBack,
            Padding = new Padding(18, 8, 18, 8)
        };

        var title = new Label
        {
            Text = AppInfo.FullTitle,
            AutoSize = false,
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 13.5F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Location = new Point(22, 14),
            Size = new Size(520, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        header.Controls.Add(title);

        var about = new Label
        {
            Text = AppInfo.AboutText,
            AutoSize = false,
            ForeColor = Color.FromArgb(178, 190, 204),
            Font = new Font(Font.FontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Location = new Point(24, 45),
            Size = new Size(520, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        header.Controls.Add(about);

        _statePill.Text = "\u5c31\u7eea";
        _statePill.TextAlign = ContentAlignment.MiddleCenter;
        _statePill.ForeColor = Color.White;
        _statePill.BackColor = Color.FromArgb(66, 78, 92);
        _statePill.Size = new Size(118, 42);
        header.Controls.Add(_statePill);

        _checkUpdateButton.Text = "\u68c0\u67e5\u66f4\u65b0";
        StyleButton(_checkUpdateButton, Color.FromArgb(66, 78, 92), Color.White);
        _checkUpdateButton.Size = new Size(136, 42);
        _checkUpdateButton.Click += async (_, _) => await CheckForUpdatesAsync(userInitiated: true);
        header.Controls.Add(_checkUpdateButton);

        _monitorButton.Text = "\u5f00\u59cb\u76d1\u63a7";
        StyleButton(_monitorButton, Accent, Color.White);
        _monitorButton.Size = new Size(160, 42);
        _monitorButton.Click += async (_, _) => await ToggleMonitorAsync();
        header.Controls.Add(_monitorButton);

        void LayoutHeader()
        {
            const int gap = 10;
            var right = header.ClientSize.Width - 18;
            _monitorButton.Location = new Point(right - _monitorButton.Width, 22);
            right = _monitorButton.Left - gap;
            _checkUpdateButton.Location = new Point(right - _checkUpdateButton.Width, 22);
            right = _checkUpdateButton.Left - gap;
            _statePill.Location = new Point(right - _statePill.Width, 22);

            var titleRight = Math.Max(260, _statePill.Left - 28);
            title.Width = Math.Max(220, titleRight - title.Left);
            about.Width = title.Width;
        }

        header.SizeChanged += (_, _) => LayoutHeader();
        header.Layout += (_, _) => LayoutHeader();
        header.HandleCreated += (_, _) => LayoutHeader();

        return header;
    }

    private Control BuildSettingsTabs()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = PageBack,
            Padding = new Padding(16, 14, 16, 12)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var nav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 12)
        };
        var navStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        nav.Controls.Add(navStack);

        AddNavButton(navStack, _navMonitorButton, "\u76d1\u63a7", () => ShowSettingsPage(0));
        AddNavButton(navStack, _navOcrButton, "OCR", () => ShowSettingsPage(1));
        AddNavButton(navStack, _navWebhookButton, "\u901a\u77e5", () => ShowSettingsPage(2));
        AddNavButton(navStack, _navLogButton, "\u65e5\u5fd7", () => ShowSettingsPage(3));

        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = PageBack;
        _pageHost.Padding = new Padding(0);

        var pages = new[]
        {
            BuildMonitorPage(),
            BuildOcrPage(),
            BuildWebhookPage(),
            BuildLogPage()
        };

        foreach (var page in pages)
        {
            page.Visible = false;
            _settingPages.Add(page);
            _pageHost.Controls.Add(page);
        }

        shell.Controls.Add(nav, 0, 0);
        shell.Controls.Add(_pageHost, 0, 1);
        ShowSettingsPage(0);
        return shell;
    }

    private Control BuildMonitorPage()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = PageBack
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 214));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var general = BuildGeneralCard();
        general.Dock = DockStyle.Fill;
        general.Margin = new Padding(0, 0, 0, 12);
        page.Controls.Add(general, 0, 0);

        page.Controls.Add(BuildMonitorSubTabs(), 0, 1);

        _monitorSubPageHost.Dock = DockStyle.Fill;
        _monitorSubPageHost.BackColor = PageBack;
        _monitorSubPageHost.Padding = new Padding(0);
        _monitorSubPageHost.SizeChanged += (_, _) => LayoutMonitorSplit();
        page.Controls.Add(_monitorSubPageHost, 0, 2);

        AddMonitorSubPage(BuildOcrMonitorPage());
        AddMonitorSubPage(BuildSingleMonitorTaskPage(BuildDialogMonitorCard()));
        AddMonitorSubPage(BuildSingleMonitorTaskPage(BuildTaskbarMonitorCard()));
        ShowMonitorSubPage(0);
        page.HandleCreated += (_, _) => BeginInvoke(new Action(LayoutMonitorSplit));
        return page;
    }

    private Control BuildOcrMonitorPage()
    {
        _monitorOcrLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PageBack,
            ColumnCount = 2,
            RowCount = 1
        };
        _monitorOcrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _monitorOcrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
        _monitorOcrLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _monitorOcrLayout.SizeChanged += (_, _) => LayoutMonitorSplit();

        _monitorTasksHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = PageBack,
            Padding = new Padding(0, 0, 8, 0)
        };
        _monitorTasksHost.SizeChanged += (_, _) => LayoutMonitorTaskCards();
        _monitorTasksHost.Layout += (_, _) => LayoutMonitorTaskCards();

        _monitorTasksHost.Controls.Add(BuildOcrMonitorCard());
        _monitorOcrLayout.Controls.Add(_monitorTasksHost, 0, 0);
        _monitorPreviewArea = BuildMonitorPreviewArea();
        _monitorPreviewArea.Visible = false;
        _monitorOcrLayout.Controls.Add(_monitorPreviewArea, 1, 0);
        return _monitorOcrLayout;
    }

    private Control BuildSingleMonitorTaskPage(Control taskCard)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PageBack,
            Padding = new Padding(0)
        };
        taskCard.Dock = DockStyle.Fill;
        taskCard.Margin = new Padding(0);
        page.Controls.Add(taskCard);
        return page;
    }

    private Control BuildMonitorSubTabs()
    {
        var nav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            Padding = new Padding(10, 9, 10, 9),
            Margin = new Padding(0, 0, 0, 12)
        };
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        nav.Controls.Add(stack);
        AddSubTabButton(stack, _monitorOcrTaskButton, "OCR \u753b\u9762", () => ShowMonitorSubPage(0));
        AddSubTabButton(stack, _monitorDialogTaskButton, "\u5f39\u7a97\u6587\u672c", () => ShowMonitorSubPage(1));
        AddSubTabButton(stack, _monitorTaskbarTaskButton, "\u4efb\u52a1\u680f\u95ea\u70c1", () => ShowMonitorSubPage(2));
        return nav;
    }

    private Control BuildLogPage()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = PageBack
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var controlCard = CreateTaskCard("\u65e5\u5fd7\u63a7\u5236", "\u63a7\u5236\u672c\u5730\u65e5\u5fd7\u7684\u6700\u5927\u884c\u6570\uff0c\u5e76\u53ef\u624b\u52a8\u5237\u65b0\u6216\u6e05\u7a7a\u3002");
        SetTaskCardPresentation(controlCard, 0);
        AddNumber(controlCard, "\u6700\u5927\u884c\u6570", _maxLogLinesInput, 100, 50000);

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _refreshLogButton.Text = "\u5237\u65b0\u65e5\u5fd7";
        _refreshLogButton.Click += (_, _) => RefreshLogViewer();
        StyleButton(_refreshLogButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        buttonRow.Controls.Add(_refreshLogButton);

        _clearLogButton.Text = "\u6e05\u7a7a\u65e5\u5fd7";
        _clearLogButton.Click += (_, _) => ClearLogs();
        StyleButton(_clearLogButton, Danger, Color.White);
        buttonRow.Controls.Add(_clearLogButton);
        AddFull(controlCard, buttonRow);
        StretchFormInputs(controlCard);
        var controlContainer = GetTaskCardContainer(controlCard);
        controlContainer.Dock = DockStyle.Fill;
        controlContainer.Margin = new Padding(0, 0, 0, 12);
        page.Controls.Add(controlContainer, 0, 0);

        var viewerCard = CreateTaskCard("\u65e5\u5fd7\u5185\u5bb9", "\u76d1\u63a7\u8fc7\u7a0b\u3001OCR \u9519\u8bef\u548c\u901a\u77e5\u53d1\u9001\u95ee\u9898\u4f1a\u8bb0\u5f55\u5728\u8fd9\u91cc\u3002");
        SetTaskCardPresentation(viewerCard, 0);
        _logViewer.Multiline = true;
        _logViewer.ReadOnly = true;
        _logViewer.ScrollBars = ScrollBars.Both;
        _logViewer.WordWrap = false;
        _logViewer.Dock = DockStyle.Fill;
        _logViewer.Margin = new Padding(0);
        _logViewer.Height = 520;
        AddFull(viewerCard, _logViewer);
        StretchFormInputs(viewerCard);
        _logViewer.Dock = DockStyle.Fill;
        var viewerContainer = GetTaskCardContainer(viewerCard);
        viewerContainer.Dock = DockStyle.Fill;
        viewerContainer.Margin = new Padding(0);
        page.Controls.Add(viewerContainer, 0, 1);

        return page;
    }

    private Control BuildGeneralCard()
    {
        var card = CreateTaskCard("\u76d1\u63a7\u603b\u63a7", "\u901a\u7528\u8282\u594f\u5bf9\u6240\u6709\u63d0\u9192\u751f\u6548\uff0c\u5404\u63d0\u9192\u7c7b\u578b\u7684\u672a\u53d8\u5316\u6b21\u6570\u72ec\u7acb\u8ba1\u7b97\u3002");
        AddNumber(card, "\u9884\u89c8\u95f4\u9694\u6beb\u79d2", _intervalInput, 100, 5000);
        AddNumber(card, "\u901a\u77e5\u51b7\u5374\u79d2\u6570", _cooldownInput, 0, 3600);
        AddNumber(card, "\u672a\u53d8\u5316\u6700\u591a\u63d0\u9192", _maxUnchangedNotifyInput, 1, 100);
        var container = GetTaskCardContainer(card);
        container.Dock = DockStyle.Fill;
        return container;
    }

    private Control BuildOcrMonitorCard()
    {
        var card = CreateTaskCard("OCR \u753b\u9762\u5173\u952e\u8bcd", "\u9002\u5408\u76d1\u63a7\u6e38\u620f\u7a97\u53e3\u6216\u684c\u9762\u533a\u57df\uff1b\u53ea\u6709\u542f\u7528\u672c\u9879\u65f6\u624d\u4f1a\u663e\u793a\u53f3\u4fa7\u9884\u89c8\u3002");
        _ocrKeywordEnabled.Text = "\u542f\u7528 OCR \u753b\u9762\u5173\u952e\u8bcd\u63d0\u9192";
        _ocrKeywordEnabled.CheckedChanged += (_, _) =>
        {
            UpdateDetectionModeVisibility();
            SaveConfigFromUi();
        };
        AddFull(card, _ocrKeywordEnabled);

        _windowList.DropDownStyle = ComboBoxStyle.DropDownList;
        var sourceLabel = AddLabeled(card, "\u753b\u9762\u6765\u6e90", _windowList);
        _sourceSelectionControls.Add(sourceLabel);
        _sourceSelectionControls.Add(_windowList);

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _refreshButton.Text = "\u5237\u65b0\u6765\u6e90";
        _refreshButton.Click += (_, _) => RefreshWindows();
        StyleButton(_refreshButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        buttonRow.Controls.Add(_refreshButton);

        _previewButton.Text = "\u6253\u5f00\u9884\u89c8";
        _previewButton.Click += (_, _) => TogglePreview();
        StyleButton(_previewButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        buttonRow.Controls.Add(_previewButton);
        AddFull(card, buttonRow);
        _sourceSelectionControls.Add(buttonRow);

        var ocrIntervalLabel = AddNumber(card, "OCR \u8bc6\u522b\u95f4\u9694\u6beb\u79d2", _keywordOcrIntervalInput, 500, 30000);
        _ocrSourceControls.Add(ocrIntervalLabel);
        _ocrSourceControls.Add(_keywordOcrIntervalInput);
        _watchKeywords.Multiline = true;
        _watchKeywords.ScrollBars = ScrollBars.Vertical;
        _watchKeywords.Height = 96;
        _watchKeywords.PlaceholderText = "\u7f51\u7edc\u9519\u8bef\r\n\u8bf7\u91cd\u65b0\u767b\u5f55\r\nxxx";
        var ocrKeywordLabel = AddLabeled(card, "OCR \u5173\u952e\u8bcd", _watchKeywords);
        _ocrSourceControls.Add(ocrKeywordLabel);
        _ocrSourceControls.Add(_watchKeywords);

        _regionLabel.AutoSize = true;
        _regionLabel.ForeColor = MutedText;
        _regionLabel.Text = "\u672a\u6846\u9009\uff0c\u5c06\u76d1\u63a7\u6240\u9009\u6765\u6e90\u7684\u5168\u90e8\u753b\u9762";
        var regionLabel = AddLabeled(card, "\u76d1\u63a7\u533a\u57df", _regionLabel);
        _regionSelectionControls.Add(regionLabel);
        _regionSelectionControls.Add(_regionLabel);

        return GetTaskCardContainer(card);
    }

    private Control BuildDialogMonitorCard()
    {
        var card = CreateTaskCard("\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c", "\u9002\u5408\u68c0\u6d4b\u201c\u7f51\u7edc\u9519\u8bef\u201d\u8fd9\u7c7b\u6807\u51c6\u7a97\u53e3\u6587\u672c\uff0c\u4e0d\u9700\u8981 OCR \u9884\u89c8\u3002");
        SetTaskCardPresentation(card, 0);
        _dialogKeywordEnabled.Text = "\u542f\u7528\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c\u5173\u952e\u8bcd\u63d0\u9192";
        _dialogKeywordEnabled.CheckedChanged += (_, _) => SaveConfigFromUi();
        AddFull(card, _dialogKeywordEnabled);
        AddNumber(card, "\u6587\u672c\u679a\u4e3e\u95f4\u9694\u6beb\u79d2", _dialogIntervalInput, 500, 30000);

        _dialogKeywords.Multiline = true;
        _dialogKeywords.ScrollBars = ScrollBars.Vertical;
        _dialogKeywords.Height = 88;
        _dialogKeywords.PlaceholderText = "\u7f51\u7edc\u9519\u8bef\r\n\u8bf7\u91cd\u65b0\u767b\u5f55";
        AddLabeled(card, "\u5f39\u7a97\u5173\u952e\u8bcd", _dialogKeywords);
        return GetTaskCardContainer(card);
    }

    private Control BuildTaskbarMonitorCard()
    {
        var card = CreateTaskCard("\u4efb\u52a1\u680f\u95ea\u70c1", "\u5148\u9009\u62e9\u8981\u76d1\u63a7\u7684\u8f6f\u4ef6\u7a97\u53e3\uff1b\u53ea\u8981\u8be5\u8f6f\u4ef6\u7684\u4efb\u52a1\u680f\u6309\u94ae\u53d1\u751f\u95ea\u70c1\u5c31\u4f1a\u63d0\u9192\u3002");
        SetTaskCardPresentation(card, 0);
        _taskbarFlashEnabled.Text = "\u542f\u7528\u6307\u5b9a\u8f6f\u4ef6\u4efb\u52a1\u680f\u95ea\u70c1\u63d0\u9192";
        _taskbarFlashEnabled.CheckedChanged += (_, _) => SaveConfigFromUi();
        AddFull(card, _taskbarFlashEnabled);

        _taskbarWindowList.DropDownStyle = ComboBoxStyle.DropDownList;
        _taskbarWindowList.SelectedIndexChanged += (_, _) => SaveConfigFromUi();
        AddLabeled(card, "\u76d1\u63a7\u8f6f\u4ef6", _taskbarWindowList);

        _refreshTaskbarWindowsButton.Text = "\u5237\u65b0\u8f6f\u4ef6\u5217\u8868";
        _refreshTaskbarWindowsButton.Click += (_, _) => RefreshWindows();
        StyleButton(_refreshTaskbarWindowsButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        AddFull(card, _refreshTaskbarWindowsButton);

        _taskbarFlashStatus.AutoSize = true;
        _taskbarFlashStatus.ForeColor = MutedText;
        _taskbarFlashStatus.Text = "\u5c1a\u672a\u6536\u5230\u4efb\u52a1\u680f\u95ea\u70c1\u4e8b\u4ef6";
        AddLabeled(card, "\u6700\u8fd1\u72b6\u6001", _taskbarFlashStatus);
        return GetTaskCardContainer(card);
    }

    private Control BuildOcrPage()
    {
        var page = CreateDashboardPage();
        var settingsCard = CreateTaskCard("OCR \u5f15\u64ce", "\u914d\u7f6e OCR \u8bc6\u522b\u65b9\u5f0f\u548c\u672c\u5730/\u8fdc\u7a0b\u8bc6\u522b\u53c2\u6570\u3002");
        _ocrEnabled.Text = "\u542f\u7528 OCR";
        _ocrEnabled.CheckedChanged += (_, _) =>
        {
            UpdateOcrModeVisibility();
            SaveConfigFromUi();
        };
        AddFull(settingsCard, _ocrEnabled);

        _ocrMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _ocrMode.Items.AddRange(["wxocr", "local", "command"]);
        _ocrMode.SelectedIndexChanged += (_, _) => UpdateOcrModeVisibility();
        AddLabeled(settingsCard, "\u6a21\u5f0f", _ocrMode);

        _ocrUrl.PlaceholderText = "http://192.168.88.3:5000/ocr";
        AddLabeled(settingsCard, "wxocr URL", _ocrUrl);

        _ocrCommand.PlaceholderText = "tesseract";
        AddLabeled(settingsCard, "\u672c\u5730\u547d\u4ee4", _ocrCommand);

        _ocrArguments.PlaceholderText = "\"{image}\" stdout -l chi_sim+eng --psm 6";
        AddLabeled(settingsCard, "\u547d\u4ee4\u53c2\u6570", _ocrArguments);

        AddNumber(settingsCard, "\u6700\u4f4e\u7f6e\u4fe1", _minConfidenceInput, 0, 100);
        AddNumber(settingsCard, "OCR \u6700\u5927\u50cf\u7d20\uff08\u4e07\uff09", _maxOcrPixelsInput, 20, 500);
        page.Controls.Add(GetTaskCardContainer(settingsCard));

        var resultCard = CreateTaskCard("\u8bc6\u522b\u7ed3\u679c", "\u6700\u8fd1\u4e00\u6b21 OCR \u6587\u672c\u548c\u5173\u952e\u8bcd\u547d\u4e2d\u4fe1\u606f\u3002");
        _ocrText.Multiline = true;
        _ocrText.ReadOnly = true;
        _ocrText.ScrollBars = ScrollBars.Vertical;
        _ocrText.Height = 200;
        _ocrText.PlaceholderText = "\u6700\u8fd1\u4e00\u6b21 OCR \u6587\u672c\u4f1a\u663e\u793a\u5728\u8fd9\u91cc";
        AddLabeled(resultCard, "\u6700\u8fd1 OCR", _ocrText);

        _keywordLastHitText.Multiline = true;
        _keywordLastHitText.ReadOnly = true;
        _keywordLastHitText.ScrollBars = ScrollBars.Vertical;
        _keywordLastHitText.Height = 120;
        _keywordLastHitText.PlaceholderText = "\u5173\u952e\u8bcd\u547d\u4e2d\u548c\u76d1\u63a7\u533a\u57df\u4f1a\u663e\u793a\u5728\u8fd9\u91cc";
        AddLabeled(resultCard, "\u547d\u4e2d\u4fe1\u606f", _keywordLastHitText);
        page.Controls.Add(GetTaskCardContainer(resultCard));

        return page;
    }

    private Control BuildWebhookPage()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = PageBack
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var listCard = CreateTaskCard("\u901a\u77e5\u6e20\u9053", "\u53ef\u540c\u65f6\u52fe\u9009\u591a\u4e2a\u6e20\u9053\uff0c\u547d\u4e2d\u540e\u4f1a\u4f9d\u6b21\u63a8\u9001\u3002");
        SetTaskCardPresentation(listCard, 0);
        _webhookChannels.CheckOnClick = true;
        _webhookChannels.Height = 220;
        _webhookChannels.SelectedIndexChanged += (_, _) => LoadSelectedWebhookChannelIntoEditor();
        _webhookChannels.ItemCheck += (_, _) =>
        {
            if (_updatingWebhookChannels)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                SyncWebhookChecksToChannels();
                SaveConfigFromUi();
            }));
        };
        AddFull(listCard, _webhookChannels);

        var channelButtons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _addWebhookChannelButton.Text = "\u65b0\u589e\u6e20\u9053";
        _addWebhookChannelButton.Click += (_, _) => AddWebhookChannel();
        StyleButton(_addWebhookChannelButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        channelButtons.Controls.Add(_addWebhookChannelButton);

        _saveWebhookChannelButton.Text = "\u4fdd\u5b58\u6e20\u9053";
        _saveWebhookChannelButton.Click += (_, _) => SaveSelectedWebhookChannelFromUi();
        StyleButton(_saveWebhookChannelButton, Accent, Color.White);
        channelButtons.Controls.Add(_saveWebhookChannelButton);

        _deleteWebhookChannelButton.Text = "\u5220\u9664\u6e20\u9053";
        _deleteWebhookChannelButton.Click += (_, _) => DeleteSelectedWebhookChannel();
        StyleButton(_deleteWebhookChannelButton, Danger, Color.White);
        channelButtons.Controls.Add(_deleteWebhookChannelButton);
        AddFull(listCard, channelButtons);

        _webhookEnabled.Text = "\u542f\u7528\u5f53\u524d\u6e20\u9053";
        _webhookEnabled.CheckedChanged += (_, _) =>
        {
            if (_updatingWebhookChannels)
            {
                return;
            }

            SyncWebhookEditorToSelectedChannel();
            RefreshWebhookChannelList(_webhookChannels.SelectedIndex);
            SaveConfigFromUi();
        };
        AddFull(listCard, _webhookEnabled);
        StretchFormInputs(listCard);
        var listContainer = GetTaskCardContainer(listCard);
        listContainer.Dock = DockStyle.Fill;
        listContainer.Margin = new Padding(0, 0, 12, 0);
        page.Controls.Add(listContainer, 0, 0);

        var editorScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = PageBack,
            Margin = new Padding(0)
        };
        page.Controls.Add(editorScroll, 1, 0);

        var editorCard = CreateTaskCard("\u6e20\u9053\u7f16\u8f91", "\u53ef\u7528\u53d8\u91cf\uff1a{title} {body} {window} {region} {ocrText} {time} {timestamp}\uff1bJSON \u5916\u53ef\u7528 Raw/Url \u53d8\u91cf\u3002");
        SetTaskCardPresentation(editorCard, 0);
        var purposeRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _webhookForOcr.Text = "OCR \u753b\u9762";
        _webhookForDialog.Text = "\u5f39\u7a97\u6587\u672c";
        _webhookForTaskbar.Text = "\u4efb\u52a1\u680f\u95ea\u70c1";
        foreach (var checkBox in new[] { _webhookForOcr, _webhookForDialog, _webhookForTaskbar })
        {
            checkBox.AutoSize = true;
            checkBox.Margin = new Padding(0, 0, 18, 10);
            checkBox.CheckedChanged += (_, _) =>
            {
                if (_updatingWebhookChannels)
                {
                    return;
                }

                SyncWebhookEditorToSelectedChannel();
                SaveConfigFromUi();
            };
            purposeRow.Controls.Add(checkBox);
        }
        AddLabeled(editorCard, "\u9002\u7528\u63d0\u9192", purposeRow);

        _webhookName.PlaceholderText = "\u4f8b\u5982\uff1a\u9489\u9489\u7fa4\u3001Bark\u624b\u673a";
        AddLabeled(editorCard, "\u6e20\u9053\u540d", _webhookName);

        _webhookPreset.DropDownStyle = ComboBoxStyle.DropDownList;
        _webhookPreset.Items.AddRange(["windows", "generic", "serverchan", "bark", "dingtalk", "feishu", "wecom", "custom"]);
        _webhookPreset.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingWebhookChannels)
            {
                return;
            }

            UpdateWebhookEditorMode(null);
        };
        AddLabeled(editorCard, "\u9884\u8bbe", _webhookPreset);

        _applyWebhookPresetButton.Text = "\u5e94\u7528\u9884\u8bbe";
        _applyWebhookPresetButton.Click += (_, _) => ApplyWebhookPreset(_webhookPreset.SelectedItem?.ToString() ?? "generic");
        StyleButton(_applyWebhookPresetButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        AddFull(editorCard, _applyWebhookPresetButton);

        _webhookMethod.PlaceholderText = "POST";
        var methodLabel = AddLabeled(editorCard, "Method", _webhookMethod);
        _webhookOnlyControls.AddRange([methodLabel, _webhookMethod]);

        _webhookUrl.PlaceholderText = "https://example.com/webhook";
        var urlLabel = AddLabeled(editorCard, "URL", _webhookUrl);
        _webhookOnlyControls.AddRange([urlLabel, _webhookUrl]);

        _webhookHeaders.Multiline = true;
        _webhookHeaders.ScrollBars = ScrollBars.Vertical;
        _webhookHeaders.Height = 74;
        _webhookHeaders.PlaceholderText = "Content-Type: application/json";
        var headersLabel = AddLabeled(editorCard, "Headers", _webhookHeaders);
        _webhookOnlyControls.AddRange([headersLabel, _webhookHeaders]);

        ConfigureBodyTemplateTextBox(_webhookBodyTemplate, "{\"title\":\"{title}\",\"body\":\"{body}\"}");
        ConfigureBodyTemplateTextBox(_webhookOcrBodyTemplate, "\u7559\u7a7a\u5219\u4f7f\u7528\u901a\u7528 Body");
        ConfigureBodyTemplateTextBox(_webhookDialogBodyTemplate, "\u7559\u7a7a\u5219\u4f7f\u7528\u901a\u7528 Body");
        ConfigureBodyTemplateTextBox(_webhookTaskbarBodyTemplate, "\u7559\u7a7a\u5219\u4f7f\u7528\u901a\u7528 Body");
        _webhookBodyTabs.Height = 250;
        _webhookBodyTabs.TabPages.Clear();
        AddBodyTemplateTab(_webhookBodyTabs, "\u901a\u7528", _webhookBodyTemplate);
        AddBodyTemplateTab(_webhookBodyTabs, "OCR", _webhookOcrBodyTemplate);
        AddBodyTemplateTab(_webhookBodyTabs, "\u5f39\u7a97", _webhookDialogBodyTemplate);
        AddBodyTemplateTab(_webhookBodyTabs, "\u95ea\u70c1", _webhookTaskbarBodyTemplate);
        AddLabeled(editorCard, "Body \u6a21\u677f", _webhookBodyTabs);

        _windowsVoiceEnabled.Text = "\u542f\u7528\u8bed\u97f3\u64ad\u62a5";
        _windowsVoiceEnabled.AutoSize = true;
        _windowsVoiceEnabled.CheckedChanged += (_, _) =>
        {
            if (_updatingWebhookChannels)
            {
                return;
            }

            SyncWebhookEditorToSelectedChannel();
            SaveConfigFromUi();
        };
        var voiceEnabledLabel = AddLabeled(editorCard, "\u672c\u5730\u8bed\u97f3", _windowsVoiceEnabled);
        _windowsOnlyControls.AddRange([voiceEnabledLabel, _windowsVoiceEnabled]);

        ConfigureBodyTemplateTextBox(_windowsOcrVoiceTemplate, "\u4f8b\u5982\uff1aOCR \u753b\u9762\u547d\u4e2d\u5173\u952e\u8bcd");
        ConfigureBodyTemplateTextBox(_windowsDialogVoiceTemplate, "\u4f8b\u5982\uff1a\u68c0\u6d4b\u5230\u7a97\u53e3\u5f39\u7a97");
        ConfigureBodyTemplateTextBox(_windowsTaskbarVoiceTemplate, "\u4f8b\u5982\uff1a\u4efb\u52a1\u680f\u95ea\u70c1\u63d0\u9192");
        _windowsVoiceTabs.Height = 156;
        _windowsVoiceTabs.TabPages.Clear();
        AddBodyTemplateTab(_windowsVoiceTabs, "OCR", _windowsOcrVoiceTemplate);
        AddBodyTemplateTab(_windowsVoiceTabs, "\u5f39\u7a97", _windowsDialogVoiceTemplate);
        AddBodyTemplateTab(_windowsVoiceTabs, "\u95ea\u70c1", _windowsTaskbarVoiceTemplate);
        var voiceTabsLabel = AddLabeled(editorCard, "\u8bed\u97f3\u6a21\u677f", _windowsVoiceTabs);
        _windowsOnlyControls.AddRange([voiceTabsLabel, _windowsVoiceTabs]);
        StretchFormInputs(editorCard);

        var editorContainer = GetTaskCardContainer(editorCard);
        editorContainer.Dock = DockStyle.Top;
        editorContainer.MinimumSize = new Size(0, 860);
        editorContainer.Height = 820;
        editorContainer.Margin = new Padding(0, 0, 0, 12);
        editorScroll.Controls.Add(editorContainer);

        return page;
    }

    private Control BuildStatusBar()
    {
        var statusPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            Padding = new Padding(18, 0, 18, 0)
        };
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = MutedText;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Text = "\u5c31\u7eea";
        statusPanel.Controls.Add(_statusLabel);
        return statusPanel;
    }

    private Control BuildMonitorPreviewArea()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = PageBack,
            Padding = new Padding(12, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "OCR \u753b\u9762\u9884\u89c8",
            ForeColor = Color.FromArgb(34, 45, 58),
            Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(title, 0, 0);

        var previewHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 24, 32),
            Padding = new Padding(1)
        };
        _preview.Dock = DockStyle.Fill;
        _previewEmptyLabel.Dock = DockStyle.Fill;
        _previewEmptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _previewEmptyLabel.ForeColor = Color.FromArgb(160, 170, 182);
        _previewEmptyLabel.Text = "\u542f\u7528 OCR \u5e76\u70b9\u51fb\u201c\u6253\u5f00\u9884\u89c8\u201d";
        _preview.SelectionChanged += (_, _) =>
        {
            UpdateRegionLabel();
            SaveConfigFromUi();
        };
        previewHost.Controls.Add(_preview);
        previewHost.Controls.Add(_previewEmptyLabel);
        _previewEmptyLabel.BringToFront();
        panel.Controls.Add(previewHost, 0, 1);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "\u5728\u9884\u89c8\u753b\u9762\u4e0a\u62d6\u62fd\u53ef\u9009\u62e9 OCR \u533a\u57df\uff1b\u4e0d\u6846\u9009\u5219\u76d1\u63a7\u6574\u4e2a\u6765\u6e90\u3002"
        };
        panel.Controls.Add(hint, 0, 2);

        return panel;
    }

    private void LoadConfigIntoUi()
    {
        _loadingConfig = true;
        _intervalInput.Value = Math.Clamp(_config.PollIntervalMs, 100, 5000);
        _cooldownInput.Value = Math.Clamp(_config.CooldownSeconds, 0, 3600);
        _maxUnchangedNotifyInput.Value = Math.Clamp(_config.MaxUnchangedNotifications, 1, 100);
        _ocrKeywordEnabled.Checked = _config.OcrKeywordEnabled;
        _dialogKeywordEnabled.Checked = _config.DialogKeywordEnabled;
        _taskbarFlashEnabled.Checked = _config.TaskbarFlashEnabled;

        _ocrEnabled.Checked = _config.OcrEnabled;
        _ocrMode.SelectedItem = ResolveOcrMode(_config.OcrMode);
        _ocrUrl.Text = _config.OcrUrl;
        _ocrCommand.Text = _config.OcrCommand;
        _ocrArguments.Text = _config.OcrArguments;
        _minConfidenceInput.Value = (decimal)Math.Clamp(_config.MinConfidence * 100, 0, 100);
        var maxOcrPixels = _config.MaxOcrPixels == 2_000_000 ? 800_000 : _config.MaxOcrPixels;
        _maxOcrPixelsInput.Value = Math.Clamp(maxOcrPixels / 10_000, 20, 500);
        _keywordOcrIntervalInput.Value = Math.Clamp(_config.KeywordOcrIntervalMs, 500, 30000);
        _dialogIntervalInput.Value = Math.Clamp(_config.DialogPollIntervalMs, 500, 30000);
        _watchKeywords.Text = string.IsNullOrWhiteSpace(_config.OcrKeywords) ? _config.WatchKeywords : _config.OcrKeywords;
        _dialogKeywords.Text = string.IsNullOrWhiteSpace(_config.DialogKeywords) ? _config.WatchKeywords : _config.DialogKeywords;
        _taskbarFlashKeywords.Text = _config.TaskbarFlashKeywords;

        LoadNotificationChannelsFromConfig();
        _maxLogLinesInput.Value = Math.Clamp(_config.MaxLogLines, 100, 50000);

        if (_config.Region.Width > 0 && _config.Region.Height > 0)
        {
            _preview.SetRegion(_config.Region.ToRectangle());
        }

        _loadingConfig = false;
        UpdateRegionLabel();
        UpdateOcrModeVisibility();
        UpdateDetectionModeVisibility();
        ResetOcrResultViews();
    }

    private void RefreshWindows()
    {
        var windows = Win32Window.ListVisibleWindows();
        _windowList.Items.Clear();
        foreach (var window in windows)
        {
            _windowList.Items.Add(window);
        }

        var selected = windows.FirstOrDefault(window =>
            (!string.IsNullOrEmpty(_config.WindowTitle) && window.Title == _config.WindowTitle) ||
            (!string.IsNullOrEmpty(_config.WindowClassName) && window.ClassName == _config.WindowClassName));
        if (selected is not null)
        {
            _windowList.SelectedItem = selected;
        }
        else if (_windowList.Items.Count > 0)
        {
            _windowList.SelectedIndex = 0;
        }

        var taskbarTargets = Win32Window.ListVisibleWindowSignals();
        _taskbarWindowList.Items.Clear();
        foreach (var target in taskbarTargets)
        {
            _taskbarWindowList.Items.Add(target);
        }

        var selectedTaskbarTarget = taskbarTargets.FirstOrDefault(target =>
            MatchesConfiguredTaskbarTarget(target, _config.TaskbarFlashWindowTitle, _config.TaskbarFlashWindowClassName, _config.TaskbarFlashProcessName));
        if (selectedTaskbarTarget is not null)
        {
            _taskbarWindowList.SelectedItem = selectedTaskbarTarget;
        }
        else if (_taskbarWindowList.Items.Count > 0)
        {
            _taskbarWindowList.SelectedIndex = 0;
        }

        var windowCount = windows.Count(window => !window.IsDesktopSource);
        var desktopSourceCount = windows.Count(window => window.IsDesktopSource);
        SetStatus($"\u5df2\u627e\u5230 {windowCount} \u4e2a OCR \u6765\u6e90\uff0c{desktopSourceCount} \u4e2a\u684c\u9762/\u663e\u793a\u5668\uff0c{taskbarTargets.Count} \u4e2a\u4efb\u52a1\u680f\u76ee\u6807");
    }

    private void TogglePreview()
    {
        if (_previewing)
        {
            _previewing = false;
            if (!_monitoring)
            {
                _timer.Stop();
            }
            _preview.Image = null;
            _previewEmptyLabel.Visible = true;
            SetStatus("\u9884\u89c8\u5df2\u505c\u6b62");
            UpdateActionButtons();
            return;
        }

        if (!_ocrKeywordEnabled.Checked)
        {
            MessageBox.Show(this, "\u9884\u89c8\u53ea\u7528\u4e8e OCR \u753b\u9762\u76d1\u63a7\uff0c\u8bf7\u5148\u542f\u7528 OCR \u753b\u9762\u5173\u952e\u8bcd\u63d0\u9192\u3002", "\u9700\u8981 OCR \u9884\u89c8", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!TrySelectWindow())
        {
            return;
        }

        SaveConfigFromUi();
        _previewing = true;
        ApplyTimerInterval();
        _timer.Start();
        SetStatus(_selectedWindow?.IsDesktopSource == true ? $"\u6b63\u5728\u5171\u4eab\u9884\u89c8{_selectedWindow.Title}\uff0c\u8bf7\u5728\u53f3\u4fa7\u6846\u9009\u8981\u76d1\u63a7\u7684\u684c\u9762\u533a\u57df" : "\u6b63\u5728\u5171\u4eab\u9884\u89c8\u7a97\u53e3");
        UpdateActionButtons();
    }

    private async Task ToggleMonitorAsync()
    {
        if (_monitoring)
        {
            _monitoring = false;
            if (!_previewing)
            {
                _timer.Stop();
            }
            SaveConfigFromUi();
            SetStatus("\u76d1\u63a7\u5df2\u505c\u6b62");
            UpdateActionButtons();
            return;
        }

        if (!ValidateMonitorTasks())
        {
            return;
        }

        var ocrKeywordMode = _ocrKeywordEnabled.Checked;
        if (ocrKeywordMode && !TrySelectWindow())
        {
            return;
        }

        if (ocrKeywordMode && !ValidateOcrSettingsForMonitor())
        {
            return;
        }

        SaveConfigFromUi();
        _monitoring = true;
        _previewing = ocrKeywordMode;
        if (!ocrKeywordMode)
        {
            _preview.Image = null;
            _previewEmptyLabel.Visible = true;
        }

        _lastKeywordOcrAt = DateTimeOffset.MinValue;
        _lastDialogKeywordAt = DateTimeOffset.MinValue;
        _ocrAlertState.Reset();
        _dialogAlertState.Reset();
        _taskbarAlertState.Reset();
        ResetOcrResultViews();
        ApplyTimerInterval();
        _timer.Start();
        SetStatus($"\u76d1\u63a7\u5df2\u542f\u52a8\uff1a{DescribeEnabledTasks()}");
        UpdateActionButtons();

        await Task.CompletedTask;
    }

    private bool ValidateMonitorTasks()
    {
        var anyEnabled = _ocrKeywordEnabled.Checked || _dialogKeywordEnabled.Checked || _taskbarFlashEnabled.Checked;
        if (!anyEnabled)
        {
            MessageBox.Show(this, "\u8bf7\u81f3\u5c11\u542f\u7528\u4e00\u7c7b\u63d0\u9192\u540e\u518d\u5f00\u59cb\u76d1\u63a7\u3002", "\u9700\u8981\u63d0\u9192\u9879", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (_ocrKeywordEnabled.Checked)
        {
            if (!_ocrEnabled.Checked)
            {
                MessageBox.Show(this, "OCR \u753b\u9762\u5173\u952e\u8bcd\u63d0\u9192\u9700\u8981\u5f00\u542f OCR\u3002", "\u9700\u8981 OCR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (GetOcrKeywords().Count == 0)
            {
                MessageBox.Show(this, "\u8bf7\u4e3a OCR \u753b\u9762\u5173\u952e\u8bcd\u63d0\u9192\u81f3\u5c11\u586b\u4e00\u4e2a\u5173\u952e\u8bcd\u3002", "\u9700\u8981\u5173\u952e\u8bcd", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }

        if (_dialogKeywordEnabled.Checked && GetDialogKeywords().Count == 0)
        {
            MessageBox.Show(this, "\u8bf7\u4e3a\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c\u63d0\u9192\u81f3\u5c11\u586b\u4e00\u4e2a\u5173\u952e\u8bcd\u3002", "\u9700\u8981\u5173\u952e\u8bcd", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (_taskbarFlashEnabled.Checked && GetSelectedTaskbarTarget() is null)
        {
            MessageBox.Show(this, "\u8bf7\u5148\u9009\u62e9\u8981\u76d1\u63a7\u4efb\u52a1\u680f\u95ea\u70c1\u7684\u8f6f\u4ef6\u3002", "\u9700\u8981\u76d1\u63a7\u8f6f\u4ef6", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        return true;
    }

    private string DescribeEnabledTasks()
    {
        var tasks = new List<string>();
        if (_ocrKeywordEnabled.Checked)
        {
            tasks.Add($"OCR \u753b\u9762\uff08{GetSelectedSourceTitle()}\uff09");
        }

        if (_dialogKeywordEnabled.Checked)
        {
            tasks.Add("\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c");
        }

        if (_taskbarFlashEnabled.Checked)
        {
            tasks.Add($"\u4efb\u52a1\u680f\u95ea\u70c1\uff08{GetSelectedTaskbarTarget()?.ProcessName ?? "\u672a\u9009\u62e9"}\uff09");
        }

        return string.Join("\uff0c", tasks);
    }

    private async void TimerOnTick(object? sender, EventArgs e)
    {
        if (_closing || _polling)
        {
            return;
        }

        var needsPreview = ShouldRenderPreviewFrame() && _ocrKeywordEnabled.Checked;
        var needsOcr = _monitoring && ShouldRunOcrKeywordCheck();
        var needsDialog = _monitoring && ShouldRunDialogKeywordCheck();
        if (!needsPreview && !needsOcr && !needsDialog)
        {
            return;
        }

        _polling = true;
        try
        {
            if (needsDialog)
            {
                if (!await TryNotifyDialogKeywordAsync())
                {
                    SetStatus("\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c\u76d1\u63a7\u4e2d\uff0c\u672a\u547d\u4e2d");
                }
            }

            if (!needsPreview && !needsOcr)
            {
                return;
            }

            var captureWindow = _selectedWindow;
            if (captureWindow is null)
            {
                return;
            }

            using var capture = Win32Window.Capture(captureWindow);
            if (needsPreview)
            {
                _preview.Image = (Bitmap)capture.Clone();
                _previewEmptyLabel.Visible = false;
            }
            else if (WindowState == FormWindowState.Minimized && _preview.Image is not null)
            {
                _preview.Image = null;
                _previewEmptyLabel.Visible = true;
            }

            if (!needsOcr)
            {
                return;
            }

            await TryNotifyKeywordAsync(capture);
        }
        catch (Exception ex)
        {
            LogError("\u76d1\u63a7\u8f6e\u8be2\u5931\u8d25", ex);
            SetStatus(ex.Message);
        }
        finally
        {
            _polling = false;
        }
    }

    private bool ShouldRenderPreviewFrame()
    {
        return _previewing &&
               WindowState != FormWindowState.Minimized &&
               _preview.Visible &&
               _preview.Width > 0 &&
               _preview.Height > 0;
    }

    private Task<OcrReadResult> ReadOcrAsync(Bitmap crop)
    {
        if (string.Equals(_ocrMode.SelectedItem?.ToString(), "local", StringComparison.OrdinalIgnoreCase))
        {
            return _localOcrService.ReadLatestMessageAsync(crop);
        }

        if (string.Equals(_ocrMode.SelectedItem?.ToString(), "command", StringComparison.OrdinalIgnoreCase))
        {
            return _commandOcrService.ReadLatestMessageAsync(crop, _ocrCommand.Text.Trim(), _ocrArguments.Text.Trim());
        }

        return _wxOcrService.ReadLatestMessageAsync(crop, _ocrUrl.Text.Trim(), (double)_minConfidenceInput.Value / 100d);
    }

    private async Task<bool> TryNotifyDialogKeywordAsync()
    {
        var keywords = GetDialogKeywords();
        if (keywords.Count == 0)
        {
            return false;
        }

        _lastDialogKeywordAt = DateTimeOffset.Now;
        var dialog = Win32Window.FindVisibleDialogByKeywords(null, keywords);
        if (dialog is null)
        {
            _dialogAlertState.Reset();
            SetStatus("\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c\u76d1\u63a7\u4e2d\uff0c\u672a\u547d\u4e2d");
            return true;
        }

        var dialogText = string.Join(
            Environment.NewLine,
            new[] { dialog.Title, dialog.Text }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var hit = FindKeywordHit(dialogText, keywords);
        if (hit is null)
        {
            return false;
        }

        var normalizedHit = OcrTextParser.NormalizeText(hit);
        var signature = "dialog|" + OcrTextParser.NormalizeText(dialogText);
        var unchangedHit = string.Equals(signature, _dialogAlertState.LastSignature, StringComparison.OrdinalIgnoreCase);
        var maxUnchangedNotifications = GetMaxUnchangedNotifications();
        if (unchangedHit && _dialogAlertState.UnchangedNotifyCount >= maxUnchangedNotifications)
        {
            _keywordLastHitText.Text = $"\u6765\u6e90\uff1a\u7a97\u53e3\u5f39\u7a97{Environment.NewLine}\u547d\u4e2d\uff1a{hit}{Environment.NewLine}\u672a\u53d1\u9001\uff1a\u5f39\u7a97\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {maxUnchangedNotifications} \u6b21\u63d0\u9192\u4e0a\u9650{Environment.NewLine}{dialogText}";
            SetStatus($"\u547d\u4e2d\u5f39\u7a97\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u5f39\u7a97\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {maxUnchangedNotifications} \u6b21\u63d0\u9192\u4e0a\u9650");
            return true;
        }

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, (int)_cooldownInput.Value));
        if (normalizedHit == _dialogAlertState.LastHit && cooldown > TimeSpan.Zero && DateTimeOffset.Now - _dialogAlertState.LastNotifyAt < cooldown)
        {
            SetStatus($"\u547d\u4e2d\u5f39\u7a97\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u51b7\u5374\u4e2d");
            return true;
        }

        _dialogAlertState.LastHit = normalizedHit;
        _dialogAlertState.LastSignature = signature;
        _dialogAlertState.LastNotifyAt = DateTimeOffset.Now;
        _ocrText.Text = dialogText;
        _keywordLastHitText.Text = $"\u6765\u6e90\uff1a\u7a97\u53e3\u5f39\u7a97{Environment.NewLine}\u8fdb\u7a0b\uff1a{dialog.ProcessId}{Environment.NewLine}\u7a97\u53e3\uff1a{dialog.Title} [{dialog.ClassName}]{Environment.NewLine}\u547d\u4e2d\uff1a{hit}{Environment.NewLine}{dialogText}";
        var body = $"\u68c0\u6d4b\u5230\u7a97\u53e3\u5f39\u7a97\u547d\u4e2d\u5173\u952e\u8bcd\uff1a{hit}{Environment.NewLine}\u7a97\u53e3\uff1a{dialog.Title} [{dialog.ClassName}]{Environment.NewLine}{dialogText}";
        SetStatus($"\u547d\u4e2d\u5f39\u7a97\u5173\u952e\u8bcd\uff1a{hit}");
        SaveConfigFromUi();

        try
        {
            var notified = await _notificationService.NotifyAsync(
                AppInfo.NotificationTitle,
                body,
                _config,
                new NotificationEvent(dialog.Title, 0, Rectangle.Empty, dialogText),
                NotificationKind.DialogKeyword);
            if (!notified)
            {
                SetStatus("\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u5931\u8d25\uff1a\u6240\u6709\u901a\u77e5\u6e20\u9053\u5747\u53d1\u9001\u5931\u8d25");
                LogWarn($"\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u672a\u6210\u529f\uff1a{hit}\uff1b\u672a\u6d88\u8017\u672a\u53d8\u5316\u63d0\u9192\u6b21\u6570");
                return true;
            }

            _dialogAlertState.UnchangedNotifyCount = unchangedHit ? _dialogAlertState.UnchangedNotifyCount + 1 : 1;
            LogInfo($"\u5f39\u7a97\u5173\u952e\u8bcd\u5df2\u63d0\u9192\uff1a{hit}\uff1b\u5f53\u524d\u672a\u53d8\u5316\u8fde\u7eed\u63d0\u9192 {_dialogAlertState.UnchangedNotifyCount}/{maxUnchangedNotifications}");
            return true;
        }
        catch (Exception ex)
        {
            LogError("\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u53d1\u9001\u5931\u8d25", ex);
            SetStatus($"\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u5931\u8d25\uff1a{ex.Message}");
            return true;
        }
    }

    private async void HandleTaskbarFlash(IntPtr hwnd)
    {
        if (!_monitoring || !_taskbarFlashEnabled.Checked)
        {
            return;
        }

        var info = Win32Window.GetWindowSignalInfo(hwnd);
        if (info is null)
        {
            return;
        }

        if (!IsSelectedTaskbarTarget(info))
        {
            return;
        }

        var now = DateTimeOffset.Now;
        if (hwnd == _lastTaskbarFlashEventHandle && now - _lastTaskbarFlashEventAt < TimeSpan.FromMilliseconds(800))
        {
            return;
        }

        _lastTaskbarFlashEventHandle = hwnd;
        _lastTaskbarFlashEventAt = now;
        LogInfo($"\u6536\u5230\u4efb\u52a1\u680f\u95ea\u70c1\u4e8b\u4ef6\uff1a{info.Title} [{info.ProcessName}]");

        _taskbarAlertState.LastNotifyAt = now;
        var detail = $"\u7a97\u53e3\uff1a{info.Title}{Environment.NewLine}\u7c7b\u540d\uff1a{info.ClassName}{Environment.NewLine}\u8fdb\u7a0b\uff1a{info.ProcessName} ({info.ProcessId})";
        _taskbarFlashStatus.Text = $"\u6700\u8fd1\u95ea\u70c1\uff1a{info.Title} [{info.ProcessName}]";
        _keywordLastHitText.Text = $"\u6765\u6e90\uff1a\u4efb\u52a1\u680f\u95ea\u70c1{Environment.NewLine}{detail}";
        SetStatus($"\u4efb\u52a1\u680f\u95ea\u70c1\uff1a{info.ProcessName}");
        SaveConfigFromUi();

        try
        {
            var notified = await _notificationService.NotifyAsync(
                AppInfo.NotificationTitle,
                $"\u68c0\u6d4b\u5230\u4efb\u52a1\u680f\u7a97\u53e3\u95ea\u70c1{Environment.NewLine}{detail}",
                _config,
                new NotificationEvent(info.Title, 0, Rectangle.Empty, info.SearchText),
                NotificationKind.TaskbarFlash);
            if (!notified)
            {
                SetStatus("\u4efb\u52a1\u680f\u95ea\u70c1\u901a\u77e5\u5931\u8d25\uff1a\u6240\u6709\u901a\u77e5\u6e20\u9053\u5747\u53d1\u9001\u5931\u8d25");
                LogWarn($"\u4efb\u52a1\u680f\u95ea\u70c1\u901a\u77e5\u672a\u6210\u529f\uff1a{info.Title}");
                return;
            }

            LogInfo($"\u4efb\u52a1\u680f\u95ea\u70c1\u5df2\u63d0\u9192\uff1a{info.Title}");
        }
        catch (Exception ex)
        {
            LogError("\u4efb\u52a1\u680f\u95ea\u70c1\u901a\u77e5\u53d1\u9001\u5931\u8d25", ex);
            SetStatus($"\u4efb\u52a1\u680f\u95ea\u70c1\u901a\u77e5\u5931\u8d25\uff1a{ex.Message}");
        }
    }

    private bool ValidateOcrSettingsForMonitor()
    {
        if (!_ocrEnabled.Checked ||
            !string.Equals(_ocrMode.SelectedItem?.ToString(), "command", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (CommandOcrService.TryResolveCommand(_ocrCommand.Text, out _, out var error))
        {
            return true;
        }

        var message = error + Environment.NewLine + Environment.NewLine +
                      "\u4f60\u5df2\u6709 wxocr \u670d\u52a1\u65f6\uff0c\u5efa\u8bae\u628a OCR \u6a21\u5f0f\u5207\u6362\u4e3a wxocr\uff0cURL \u586b\u5199 http://192.168.88.3:5000/ocr\u3002";
        MessageBox.Show(this, message, "OCR \u547d\u4ee4\u4e0d\u53ef\u7528", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        LogWarn(error);
        SetStatus(error);
        return false;
    }

    private static string ResolveOcrMode(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "local" => "local",
            "command" => "command",
            _ => "wxocr"
        };
    }

    private bool ShouldRunOcrKeywordCheck()
    {
        if (!_ocrKeywordEnabled.Checked || !_ocrEnabled.Checked || _selectedWindow is null || GetOcrKeywords().Count == 0)
        {
            return false;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(500, (int)_keywordOcrIntervalInput.Value));
        return DateTimeOffset.Now - _lastKeywordOcrAt >= interval;
    }

    private bool ShouldRunDialogKeywordCheck()
    {
        if (!_dialogKeywordEnabled.Checked || GetDialogKeywords().Count == 0)
        {
            return false;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(500, (int)_dialogIntervalInput.Value));
        return DateTimeOffset.Now - _lastDialogKeywordAt >= interval;
    }

    private async Task<bool> TryNotifyKeywordAsync(Bitmap capture)
    {
        _lastKeywordOcrAt = DateTimeOffset.Now;
        var region = GetActiveMonitorRegion(capture);

        OcrReadResult ocrResult;
        try
        {
            using var ocrBitmap = CaptureMonitorRegion(capture, region);
            using var preparedBitmap = PrepareBitmapForOcr(ocrBitmap);
            ocrResult = await ReadOcrAsync(preparedBitmap);
        }
        catch (Exception ex)
        {
            LogError("OCR \u8bc6\u522b\u5931\u8d25", ex);
            SetStatus($"OCR \u8bc6\u522b\u5931\u8d25\uff1a{ex.Message}");
            return false;
        }

        var context = TrimForNotification(ocrResult.FullText, 1200);
        _ocrText.Text = string.IsNullOrWhiteSpace(context)
            ? "\u672c\u6b21 OCR \u672a\u8bc6\u522b\u5230\u6587\u672c"
            : context;

        var hit = FindKeywordHit(ocrResult.FullText, GetOcrKeywords());
        if (hit is null)
        {
            _ocrAlertState.Reset();
            _keywordLastHitText.Text = string.IsNullOrWhiteSpace(context)
                ? $"\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u672a\u547d\u4e2d\uff1a\u672c\u6b21 OCR \u672a\u8bc6\u522b\u5230\u6587\u672c"
                : $"\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u672a\u547d\u4e2d{Environment.NewLine}{context}";
            SetStatus($"\u5173\u952e\u8bcd\u76d1\u63a7\u4e2d\uff0c\u672a\u547d\u4e2d\uff1a{GetSelectedSourceTitle()}\uff1b{DescribeRegion(region, capture.Size)}");
            return false;
        }

        var normalizedHit = OcrTextParser.NormalizeText(hit);
        var signature = BuildKeywordSignature(normalizedHit, ocrResult.FullText);
        var unchangedHit = string.Equals(signature, _ocrAlertState.LastSignature, StringComparison.OrdinalIgnoreCase);
        var maxUnchangedNotifications = GetMaxUnchangedNotifications();
        if (unchangedHit && _ocrAlertState.UnchangedNotifyCount >= maxUnchangedNotifications)
        {
            _keywordLastHitText.Text = $"\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u533a\u57df\uff1a{DescribeRegion(region, capture.Size)}{Environment.NewLine}\u547d\u4e2d\uff1a{hit}{Environment.NewLine}\u672a\u53d1\u9001\uff1a\u753b\u9762\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {maxUnchangedNotifications} \u6b21\u63d0\u9192\u4e0a\u9650{Environment.NewLine}{context}";
            SetStatus($"\u547d\u4e2d\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u753b\u9762\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {maxUnchangedNotifications} \u6b21\u63d0\u9192\u4e0a\u9650");
            return false;
        }

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, (int)_cooldownInput.Value));
        if (normalizedHit == _ocrAlertState.LastHit && cooldown > TimeSpan.Zero && DateTimeOffset.Now - _ocrAlertState.LastNotifyAt < cooldown)
        {
            SetStatus($"\u547d\u4e2d\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u51b7\u5374\u4e2d");
            return false;
        }

        _ocrAlertState.LastHit = normalizedHit;
        _ocrAlertState.LastSignature = signature;
        _ocrAlertState.LastNotifyAt = DateTimeOffset.Now;

        _keywordLastHitText.Text = $"\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u533a\u57df\uff1a{DescribeRegion(region, capture.Size)}{Environment.NewLine}\u547d\u4e2d\uff1a{hit}{Environment.NewLine}{context}";
        var body = $"\u76d1\u63a7\u753b\u9762 OCR \u547d\u4e2d\u5173\u952e\u8bcd\uff1a{hit}{Environment.NewLine}\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u533a\u57df\uff1a{DescribeRegion(region, capture.Size)}{Environment.NewLine}{context}";
        SetStatus($"\u547d\u4e2d\u5173\u952e\u8bcd\uff1a{hit}");
        SaveConfigFromUi();

        try
        {
            var notified = await _notificationService.NotifyAsync(
                AppInfo.NotificationTitle,
                body,
                _config,
                new NotificationEvent(_selectedWindow?.Title ?? "", 0, region, ocrResult.FullText),
                NotificationKind.OcrKeyword);
            if (!notified)
            {
                SetStatus("\u5173\u952e\u8bcd\u901a\u77e5\u5931\u8d25\uff1a\u6240\u6709\u901a\u77e5\u6e20\u9053\u5747\u53d1\u9001\u5931\u8d25");
                LogWarn($"\u5173\u952e\u8bcd\u901a\u77e5\u672a\u6210\u529f\uff1a{hit}\uff1b\u672a\u6d88\u8017\u672a\u53d8\u5316\u63d0\u9192\u6b21\u6570");
                return false;
            }

            _ocrAlertState.UnchangedNotifyCount = unchangedHit ? _ocrAlertState.UnchangedNotifyCount + 1 : 1;
            LogInfo($"\u5173\u952e\u8bcd\u5df2\u63d0\u9192\uff1a{hit}\uff1b\u5f53\u524d\u672a\u53d8\u5316\u8fde\u7eed\u63d0\u9192 {_ocrAlertState.UnchangedNotifyCount}/{maxUnchangedNotifications}");
            return true;
        }
        catch (Exception ex)
        {
            LogError("\u5173\u952e\u8bcd\u901a\u77e5\u53d1\u9001\u5931\u8d25", ex);
            SetStatus($"\u5173\u952e\u8bcd\u901a\u77e5\u5931\u8d25\uff1a{ex.Message}");
            return false;
        }
    }

    private void ResetKeywordNotificationState()
    {
        _ocrAlertState.Reset();
        _dialogAlertState.Reset();
        _taskbarAlertState.Reset();
    }

    private static string BuildKeywordSignature(string normalizedHit, string ocrText)
    {
        return normalizedHit + "|" + OcrTextParser.NormalizeText(ocrText);
    }

    private Rectangle GetActiveMonitorRegion(Bitmap capture)
    {
        var full = new Rectangle(Point.Empty, capture.Size);
        var region = Rectangle.Intersect(_preview.SelectedRegion, full);
        return region.Width > 0 && region.Height > 0 ? region : full;
    }

    private static Bitmap CaptureMonitorRegion(Bitmap capture, Rectangle region)
    {
        var full = new Rectangle(Point.Empty, capture.Size);
        region = Rectangle.Intersect(region, full);
        if (region == full)
        {
            return (Bitmap)capture.Clone();
        }

        return capture.Clone(region, capture.PixelFormat);
    }

    private Bitmap PrepareBitmapForOcr(Bitmap bitmap)
    {
        var maxPixels = Math.Max(200_000, (int)_maxOcrPixelsInput.Value * 10_000);
        var pixels = (long)bitmap.Width * bitmap.Height;
        if (pixels <= maxPixels)
        {
            return CloneAs24Bpp(bitmap, bitmap.Width, bitmap.Height);
        }

        var scale = Math.Sqrt((double)maxPixels / pixels);
        var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
        var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
        return CloneAs24Bpp(bitmap, width, height);
    }

    private static Bitmap CloneAs24Bpp(Bitmap source, int width, int height)
    {
        var target = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(target);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        return target;
    }

    private string GetSelectionModeText()
    {
        if (_preview.SelectedRegion.Width <= 0 || _preview.SelectedRegion.Height <= 0)
        {
            return "\u672a\u6846\u9009\uff0c\u76d1\u63a7\u6574\u4e2a\u6240\u9009\u6765\u6e90";
        }

        var region = _preview.SelectedRegion;
        return $"\u6846\u9009\u533a\u57df x={region.X}, y={region.Y}, w={region.Width}, h={region.Height}";
    }

    private static string DescribeRegion(Rectangle region, Size captureSize)
    {
        return region.Location == Point.Empty && region.Size == captureSize
            ? "\u6574\u4e2a\u6240\u9009\u6765\u6e90"
            : $"x={region.X}, y={region.Y}, w={region.Width}, h={region.Height}";
    }

    private IReadOnlyList<string> GetOcrKeywords() => GetKeywords(_watchKeywords);

    private IReadOnlyList<string> GetDialogKeywords() => GetKeywords(_dialogKeywords);

    private IReadOnlyList<string> GetTaskbarFlashKeywords() => GetKeywords(_taskbarFlashKeywords);

    private int GetMaxUnchangedNotifications() => Math.Clamp((int)_maxUnchangedNotifyInput.Value, 1, 100);

    private static IReadOnlyList<string> GetKeywords(TextBox textBox)
    {
        return textBox.Text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(OcrTextParser.NormalizeText)
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? FindKeywordHit(string text, IReadOnlyList<string> keywords)
    {
        var normalizedText = OcrTextParser.NormalizeText(text);
        foreach (var keyword in keywords)
        {
            if (normalizedText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return keyword;
            }
        }

        return null;
    }

    private static string TrimForNotification(string text, int maxLength)
    {
        var normalized = string.Join(Environment.NewLine, text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "...";
    }

    private bool TrySelectWindow()
    {
        if (_windowList.SelectedItem is not WindowInfo window)
        {
            MessageBox.Show(this, "\u8bf7\u5148\u9009\u62e9\u8981\u5171\u4eab\u6216\u76d1\u63a7\u7684\u6765\u6e90\uff0c\u53ef\u4ee5\u9009\u201c\u6574\u4e2a\u684c\u9762\u201d\u6216\u67d0\u4e2a\u7a97\u53e3\u3002", "\u9700\u8981\u76d1\u63a7\u6765\u6e90", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        _selectedWindow = window;
        return true;
    }

    private string GetSelectedSourceTitle()
    {
        return _selectedWindow?.Title ?? (_windowList.SelectedItem as WindowInfo)?.Title ?? "\u672a\u9009\u62e9";
    }

    private WindowSignalInfo? GetSelectedTaskbarTarget()
    {
        return _taskbarWindowList.SelectedItem as WindowSignalInfo;
    }

    private static bool MatchesConfiguredTaskbarTarget(
        WindowSignalInfo target,
        string title,
        string className,
        string processName)
    {
        if (!string.IsNullOrWhiteSpace(processName) &&
            !string.Equals(target.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(className) &&
            !string.Equals(target.ClassName, className, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(title) ||
               string.Equals(target.Title, title, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSelectedTaskbarTarget(WindowSignalInfo info)
    {
        var target = GetSelectedTaskbarTarget();
        if (target is null)
        {
            return false;
        }

        if (info.Handle == target.Handle)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.ProcessName) &&
            string.Equals(info.ProcessName, target.ProcessName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(target.ClassName) &&
               string.Equals(info.ClassName, target.ClassName, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadNotificationChannelsFromConfig()
    {
        _notificationChannels.Clear();
        _notificationChannels.AddRange(_config.GetEffectiveNotificationChannels().Select(channel => channel.Clone()));
        if (_notificationChannels.Count == 0)
        {
            _notificationChannels.Add(CreateDefaultWindowsNotificationChannel());
            _notificationChannels.Add(CreateDefaultNotificationChannel());
        }
        else if (!_config.WindowsNotificationChannelMigrated && _notificationChannels.All(channel => !channel.IsWindowsLocal))
        {
            _notificationChannels.Insert(0, CreateDefaultWindowsNotificationChannel());
        }

        _config.WindowsNotificationChannelMigrated = true;

        RefreshWebhookChannelList(0);
    }

    private static NotificationChannelConfig CreateDefaultWindowsNotificationChannel()
    {
        return new NotificationChannelConfig
        {
            Name = "\u0057\u0069\u006e\u0064\u006f\u0077\u0073 \u672c\u5730\u63d0\u9192",
            ChannelType = "windows",
            Enabled = true,
            UseForOcrKeyword = true,
            UseForDialogKeyword = true,
            UseForTaskbarFlash = true,
            Preset = "windows",
            Method = "",
            Url = "",
            Headers = "",
            BodyTemplate = "{body}",
            OcrBodyTemplate = "\u004f\u0043\u0052 \u753b\u9762\u63d0\u9192\uff1a{body}",
            DialogBodyTemplate = "\u5f39\u7a97\u6587\u672c\u63d0\u9192\uff1a{body}",
            TaskbarBodyTemplate = "\u4efb\u52a1\u680f\u95ea\u70c1\u63d0\u9192\uff1a{body}",
            VoiceEnabled = true,
            OcrVoiceTemplate = "\u004f\u0043\u0052 \u753b\u9762\u547d\u4e2d\u5173\u952e\u8bcd",
            DialogVoiceTemplate = "\u68c0\u6d4b\u5230\u7a97\u53e3\u5f39\u7a97\u5173\u952e\u8bcd",
            TaskbarVoiceTemplate = "\u68c0\u6d4b\u5230\u4efb\u52a1\u680f\u95ea\u70c1"
        };
    }

    private static NotificationChannelConfig CreateDefaultNotificationChannel()
    {
        return new NotificationChannelConfig
        {
            Name = "Webhook",
            ChannelType = "webhook",
            Enabled = false,
            UseForOcrKeyword = true,
            UseForDialogKeyword = true,
            UseForTaskbarFlash = true,
            Preset = "generic",
            Method = "POST",
            Headers = "Content-Type: application/json",
            BodyTemplate = "{\"title\":\"{title}\",\"body\":\"{body}\",\"createdAt\":\"{time}\",\"window\":\"{window}\",\"distance\":{distance},\"region\":\"{region}\"}",
            OcrBodyTemplate = "{\"title\":\"OCR画面提醒\",\"body\":\"{body}\",\"window\":\"{window}\",\"ocrText\":\"{ocrText}\",\"time\":\"{time}\"}",
            DialogBodyTemplate = "{\"title\":\"弹窗文本提醒\",\"body\":\"{body}\",\"window\":\"{window}\",\"time\":\"{time}\"}",
            TaskbarBodyTemplate = "{\"title\":\"任务栏闪烁提醒\",\"body\":\"{body}\",\"window\":\"{window}\",\"time\":\"{time}\"}"
        };
    }

    private void RefreshWebhookChannelList(int selectedIndex = -1)
    {
        _updatingWebhookChannels = true;
        try
        {
            _webhookChannels.Items.Clear();
            for (var i = 0; i < _notificationChannels.Count; i++)
            {
                var channel = _notificationChannels[i];
                _webhookChannels.Items.Add(GetChannelDisplayName(channel, i), channel.Enabled);
            }

            if (_notificationChannels.Count == 0)
            {
                ClearWebhookEditor();
                return;
            }

            selectedIndex = selectedIndex < 0 ? 0 : Math.Clamp(selectedIndex, 0, _notificationChannels.Count - 1);
            _webhookChannels.SelectedIndex = selectedIndex;
        }
        finally
        {
            _updatingWebhookChannels = false;
        }

        LoadSelectedWebhookChannelIntoEditor();
    }

    private static string GetChannelDisplayName(NotificationChannelConfig channel, int index)
    {
        var name = string.IsNullOrWhiteSpace(channel.Name)
            ? channel.IsWindowsLocal ? "\u0057\u0069\u006e\u0064\u006f\u0077\u0073 \u672c\u5730\u63d0\u9192" : $"Webhook {index + 1}"
            : channel.Name.Trim();
        var preset = channel.IsWindowsLocal ? "windows" : string.IsNullOrWhiteSpace(channel.Preset) ? "custom" : channel.Preset;
        return $"{name}  [{preset}]";
    }

    private void LoadSelectedWebhookChannelIntoEditor()
    {
        if (_updatingWebhookChannels)
        {
            return;
        }

        if (_webhookChannels.SelectedIndex < 0 || _webhookChannels.SelectedIndex >= _notificationChannels.Count)
        {
            ClearWebhookEditor();
            return;
        }

        var channel = _notificationChannels[_webhookChannels.SelectedIndex];
        _updatingWebhookChannels = true;
        try
        {
            _webhookEnabled.Checked = channel.Enabled;
            _webhookName.Text = channel.Name;
            _webhookPreset.SelectedItem = channel.IsWindowsLocal ? "windows" : string.IsNullOrWhiteSpace(channel.Preset) ? "custom" : channel.Preset;
            if (_webhookPreset.SelectedItem is null)
            {
                _webhookPreset.SelectedItem = "custom";
            }

            _webhookMethod.Text = string.IsNullOrWhiteSpace(channel.Method) ? "POST" : channel.Method;
            _webhookUrl.Text = channel.Url;
            _webhookHeaders.Text = channel.Headers;
            _webhookBodyTemplate.Text = channel.BodyTemplate;
            _webhookOcrBodyTemplate.Text = channel.OcrBodyTemplate;
            _webhookDialogBodyTemplate.Text = channel.DialogBodyTemplate;
            _webhookTaskbarBodyTemplate.Text = channel.TaskbarBodyTemplate;
            _windowsVoiceEnabled.Checked = channel.VoiceEnabled;
            _windowsOcrVoiceTemplate.Text = channel.OcrVoiceTemplate;
            _windowsDialogVoiceTemplate.Text = channel.DialogVoiceTemplate;
            _windowsTaskbarVoiceTemplate.Text = channel.TaskbarVoiceTemplate;
            _webhookForOcr.Checked = channel.UseForOcrKeyword;
            _webhookForDialog.Checked = channel.UseForDialogKeyword;
            _webhookForTaskbar.Checked = channel.UseForTaskbarFlash;
            UpdateWebhookEditorMode(channel);
        }
        finally
        {
            _updatingWebhookChannels = false;
        }
    }

    private void ClearWebhookEditor()
    {
        _updatingWebhookChannels = true;
        try
        {
            _webhookEnabled.Checked = false;
            _webhookName.Clear();
            _webhookPreset.SelectedItem = "generic";
            _webhookMethod.Text = "POST";
            _webhookUrl.Clear();
            _webhookHeaders.Text = "Content-Type: application/json";
            _webhookBodyTemplate.Text = "{\"title\":\"{title}\",\"body\":\"{body}\"}";
            _webhookOcrBodyTemplate.Clear();
            _webhookDialogBodyTemplate.Clear();
            _webhookTaskbarBodyTemplate.Clear();
            _windowsVoiceEnabled.Checked = false;
            _windowsOcrVoiceTemplate.Clear();
            _windowsDialogVoiceTemplate.Clear();
            _windowsTaskbarVoiceTemplate.Clear();
            _webhookForOcr.Checked = true;
            _webhookForDialog.Checked = true;
            _webhookForTaskbar.Checked = true;
            UpdateWebhookEditorMode(null);
        }
        finally
        {
            _updatingWebhookChannels = false;
        }
    }

    private void UpdateWebhookEditorMode(NotificationChannelConfig? channel)
    {
        var isWindows = channel?.IsWindowsLocal ??
                        string.Equals(_webhookPreset.SelectedItem?.ToString(), "windows", StringComparison.OrdinalIgnoreCase);

        foreach (var control in _webhookOnlyControls)
        {
            control.Visible = !isWindows;
        }

        foreach (var control in _windowsOnlyControls)
        {
            control.Visible = isWindows;
        }
    }

    private static string ResolveNotificationChannelType(string preset)
    {
        return string.Equals(preset, "windows", StringComparison.OrdinalIgnoreCase) ? "windows" : "webhook";
    }

    private void SyncWebhookChecksToChannels()
    {
        for (var i = 0; i < _notificationChannels.Count && i < _webhookChannels.Items.Count; i++)
        {
            _notificationChannels[i].Enabled = _webhookChannels.GetItemChecked(i);
        }

        if (_webhookChannels.SelectedIndex >= 0 && _webhookChannels.SelectedIndex < _notificationChannels.Count)
        {
            _webhookEnabled.Checked = _notificationChannels[_webhookChannels.SelectedIndex].Enabled;
        }
    }

    private void SyncWebhookEditorToSelectedChannel()
    {
        if (_webhookChannels.SelectedIndex < 0 || _webhookChannels.SelectedIndex >= _notificationChannels.Count)
        {
            return;
        }

        var channel = _notificationChannels[_webhookChannels.SelectedIndex];
        channel.Name = string.IsNullOrWhiteSpace(_webhookName.Text) ? $"Webhook {_webhookChannels.SelectedIndex + 1}" : _webhookName.Text.Trim();
        channel.Enabled = _webhookEnabled.Checked;
        channel.Preset = _webhookPreset.SelectedItem?.ToString() ?? "custom";
        channel.ChannelType = ResolveNotificationChannelType(channel.Preset);
        var isWindows = channel.IsWindowsLocal;
        channel.Method = isWindows ? "" : string.IsNullOrWhiteSpace(_webhookMethod.Text) ? "POST" : _webhookMethod.Text.Trim();
        channel.Url = isWindows ? "" : _webhookUrl.Text.Trim();
        channel.Headers = isWindows ? "" : _webhookHeaders.Text.Trim();
        channel.BodyTemplate = _webhookBodyTemplate.Text.Trim();
        channel.OcrBodyTemplate = _webhookOcrBodyTemplate.Text.Trim();
        channel.DialogBodyTemplate = _webhookDialogBodyTemplate.Text.Trim();
        channel.TaskbarBodyTemplate = _webhookTaskbarBodyTemplate.Text.Trim();
        channel.VoiceEnabled = _windowsVoiceEnabled.Checked;
        channel.OcrVoiceTemplate = _windowsOcrVoiceTemplate.Text.Trim();
        channel.DialogVoiceTemplate = _windowsDialogVoiceTemplate.Text.Trim();
        channel.TaskbarVoiceTemplate = _windowsTaskbarVoiceTemplate.Text.Trim();
        channel.UseForOcrKeyword = _webhookForOcr.Checked;
        channel.UseForDialogKeyword = _webhookForDialog.Checked;
        channel.UseForTaskbarFlash = _webhookForTaskbar.Checked;
    }

    private void AddWebhookChannel()
    {
        SyncWebhookEditorToSelectedChannel();
        var channel = CreateDefaultNotificationChannel();
        channel.Name = $"Webhook {_notificationChannels.Count + 1}";
        _notificationChannels.Add(channel);
        RefreshWebhookChannelList(_notificationChannels.Count - 1);
        SaveConfigFromUi();
    }

    private void SaveSelectedWebhookChannelFromUi()
    {
        if (_webhookChannels.SelectedIndex < 0)
        {
            AddWebhookChannel();
            return;
        }

        var selectedIndex = _webhookChannels.SelectedIndex;
        SyncWebhookEditorToSelectedChannel();
        RefreshWebhookChannelList(selectedIndex);
        SaveConfigFromUi();
        SetStatus("\u901a\u77e5\u6e20\u9053\u5df2\u4fdd\u5b58");
    }

    private void DeleteSelectedWebhookChannel()
    {
        if (_webhookChannels.SelectedIndex < 0 || _webhookChannels.SelectedIndex >= _notificationChannels.Count)
        {
            return;
        }

        var selectedIndex = _webhookChannels.SelectedIndex;
        _notificationChannels.RemoveAt(selectedIndex);
        if (_notificationChannels.Count == 0)
        {
            _notificationChannels.Add(CreateDefaultNotificationChannel());
            selectedIndex = 0;
        }

        RefreshWebhookChannelList(Math.Min(selectedIndex, _notificationChannels.Count - 1));
        SaveConfigFromUi();
        SetStatus("\u901a\u77e5\u6e20\u9053\u5df2\u5220\u9664");
    }

    private void ApplyWebhookPreset(string preset)
    {
        _webhookPreset.SelectedItem = preset;
        if (string.Equals(preset, "custom", StringComparison.OrdinalIgnoreCase))
        {
            SaveSelectedWebhookChannelFromUi();
            return;
        }

        if (string.Equals(preset, "windows", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_webhookName.Text) || _webhookName.Text.StartsWith("Webhook ", StringComparison.OrdinalIgnoreCase))
            {
                _webhookName.Text = "\u0057\u0069\u006e\u0064\u006f\u0077\u0073 \u672c\u5730\u63d0\u9192";
            }

            _webhookMethod.Clear();
            _webhookUrl.Clear();
            _webhookHeaders.Clear();
            _webhookBodyTemplate.Text = "{body}";
            _webhookOcrBodyTemplate.Text = "\u004f\u0043\u0052 \u753b\u9762\u63d0\u9192\uff1a{body}";
            _webhookDialogBodyTemplate.Text = "\u5f39\u7a97\u6587\u672c\u63d0\u9192\uff1a{body}";
            _webhookTaskbarBodyTemplate.Text = "\u4efb\u52a1\u680f\u95ea\u70c1\u63d0\u9192\uff1a{body}";
            _windowsVoiceEnabled.Checked = true;
            _windowsOcrVoiceTemplate.Text = "\u004f\u0043\u0052 \u753b\u9762\u547d\u4e2d\u5173\u952e\u8bcd";
            _windowsDialogVoiceTemplate.Text = "\u68c0\u6d4b\u5230\u7a97\u53e3\u5f39\u7a97\u5173\u952e\u8bcd";
            _windowsTaskbarVoiceTemplate.Text = "\u68c0\u6d4b\u5230\u4efb\u52a1\u680f\u95ea\u70c1";
            UpdateWebhookEditorMode(null);
            SaveSelectedWebhookChannelFromUi();
            return;
        }

        _webhookMethod.Text = "POST";
        _webhookHeaders.Text = "Content-Type: application/json";

        _webhookBodyTemplate.Text = preset switch
        {
            "serverchan" => "{\"title\":\"{title}\",\"desp\":\"{body}\"}",
            "bark" => "{\"title\":\"{title}\",\"body\":\"{body}\",\"group\":\"梦幻西游\"}",
            "dingtalk" => "{\"msgtype\":\"text\",\"text\":{\"content\":\"{title}\\n{body}\"}}",
            "feishu" => "{\"msg_type\":\"text\",\"content\":{\"text\":\"{title}\\n{body}\"}}",
            "wecom" => "{\"msgtype\":\"text\",\"text\":{\"content\":\"{title}\\n{body}\"}}",
            _ => "{\"title\":\"{title}\",\"body\":\"{body}\",\"createdAt\":\"{time}\",\"window\":\"{window}\",\"distance\":{distance},\"region\":\"{region}\"}"
        };

        _webhookOcrBodyTemplate.Text = BuildWebhookTemplate(preset, "OCR画面提醒", "{body}");
        _webhookDialogBodyTemplate.Text = BuildWebhookTemplate(preset, "弹窗文本提醒", "{body}");
        _webhookTaskbarBodyTemplate.Text = BuildWebhookTemplate(preset, "任务栏闪烁提醒", "{body}");

        SaveSelectedWebhookChannelFromUi();
    }

    private static string BuildWebhookTemplate(string preset, string title, string body)
    {
        return preset switch
        {
            "serverchan" => $"{{\"title\":\"{title}\",\"desp\":\"{body}\"}}",
            "bark" => $"{{\"title\":\"{title}\",\"body\":\"{body}\",\"group\":\"windows-monitor\"}}",
            "dingtalk" => $"{{\"msgtype\":\"text\",\"text\":{{\"content\":\"{title}\\n{body}\"}}}}",
            "feishu" => $"{{\"msg_type\":\"text\",\"content\":{{\"text\":\"{title}\\n{body}\"}}}}",
            "wecom" => $"{{\"msgtype\":\"text\",\"text\":{{\"content\":\"{title}\\n{body}\"}}}}",
            _ => $"{{\"title\":\"{title}\",\"body\":\"{body}\",\"createdAt\":\"{{time}}\",\"window\":\"{{window}}\",\"distance\":{{distance}},\"region\":\"{{region}}\"}}"
        };
    }

    private void SaveConfigFromUi()
    {
        if (_loadingConfig)
        {
            return;
        }

        if (_windowList.SelectedItem is WindowInfo window)
        {
            _config.WindowTitle = window.Title;
            _config.WindowClassName = window.ClassName;
        }

        if (_taskbarWindowList.SelectedItem is WindowSignalInfo taskbarTarget)
        {
            _config.TaskbarFlashWindowTitle = taskbarTarget.Title;
            _config.TaskbarFlashWindowClassName = taskbarTarget.ClassName;
            _config.TaskbarFlashProcessName = taskbarTarget.ProcessName;
        }

        _config.PollIntervalMs = (int)_intervalInput.Value;
        _config.CooldownSeconds = (int)_cooldownInput.Value;
        _config.MaxUnchangedNotifications = (int)_maxUnchangedNotifyInput.Value;
        _config.MonitorMode = "keyword";
        _config.OcrKeywordEnabled = _ocrKeywordEnabled.Checked;
        _config.DialogKeywordEnabled = _dialogKeywordEnabled.Checked;
        _config.TaskbarFlashEnabled = _taskbarFlashEnabled.Checked;
        _config.KeywordDetectionMode = _dialogKeywordEnabled.Checked && !_ocrKeywordEnabled.Checked ? "windowText" : "ocr";

        _config.OcrEnabled = _ocrEnabled.Checked;
        _config.OcrMode = _ocrMode.SelectedItem?.ToString() ?? "local";
        _config.OcrUrl = _ocrUrl.Text.Trim();
        _config.OcrCommand = _ocrCommand.Text.Trim();
        _config.OcrArguments = _ocrArguments.Text.Trim();
        _config.MinConfidence = (double)_minConfidenceInput.Value / 100d;
        _config.MaxOcrPixels = (int)_maxOcrPixelsInput.Value * 10_000;
        _config.FullWindowKeywordEnabled = true;
        _config.KeywordOcrIntervalMs = (int)_keywordOcrIntervalInput.Value;
        _config.DialogPollIntervalMs = (int)_dialogIntervalInput.Value;
        _config.WatchKeywords = _watchKeywords.Text.Trim();
        _config.OcrKeywords = _watchKeywords.Text.Trim();
        _config.DialogKeywords = _dialogKeywords.Text.Trim();
        _config.TaskbarFlashKeywords = _taskbarFlashKeywords.Text.Trim();

        SyncWebhookEditorToSelectedChannel();
        _config.NotificationChannels = _notificationChannels.Select(channel => channel.Clone()).ToList();
        _config.WindowsNotificationChannelMigrated = true;
        var firstChannel = _notificationChannels.FirstOrDefault();
        _config.WebhookEnabled = firstChannel?.Enabled ?? _webhookEnabled.Checked;
        _config.WebhookPreset = firstChannel?.Preset ?? (_webhookPreset.SelectedItem?.ToString() ?? "custom");
        _config.WebhookMethod = firstChannel?.Method ?? (string.IsNullOrWhiteSpace(_webhookMethod.Text) ? "POST" : _webhookMethod.Text.Trim());
        _config.WebhookUrl = firstChannel?.Url ?? _webhookUrl.Text.Trim();
        _config.WebhookHeaders = firstChannel?.Headers ?? _webhookHeaders.Text.Trim();
        _config.WebhookBodyTemplate = firstChannel?.BodyTemplate ?? _webhookBodyTemplate.Text.Trim();
        _config.MaxLogLines = (int)_maxLogLinesInput.Value;

        _config.Region = RectDto.FromRectangle(_preview.SelectedRegion);
        _config.Save();
    }

    private void RefreshLogViewer()
    {
        _logViewer.Text = _logService.ReadAll();
        _logViewer.SelectionStart = _logViewer.TextLength;
        _logViewer.ScrollToCaret();
    }

    private void ClearLogs()
    {
        var result = MessageBox.Show(this, "\u786e\u5b9a\u8981\u6e05\u7a7a\u65e5\u5fd7\u5417\uff1f", "\u6e05\u7a7a\u65e5\u5fd7", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _logService.Clear();
        RefreshLogViewer();
        SetStatus("\u65e5\u5fd7\u5df2\u6e05\u7a7a");
    }

    private void LogInfo(string message)
    {
        _logService.Info(message, (int)_maxLogLinesInput.Value);
        RefreshLogViewer();
    }

    private void LogWarn(string message)
    {
        _logService.Warn(message, (int)_maxLogLinesInput.Value);
        RefreshLogViewer();
    }

    private void LogError(string message, Exception exception)
    {
        _logService.Error(message, exception, (int)_maxLogLinesInput.Value);
        RefreshLogViewer();
    }

    private async Task CheckForUpdatesAsync(bool userInitiated)
    {
        _checkUpdateButton.Enabled = false;
        try
        {
            if (userInitiated)
            {
                SetStatus("\u6b63\u5728\u68c0\u67e5\u66f4\u65b0...");
            }

            var updateInfo = await _updateService.CheckLatestAsync();
            if (updateInfo is null)
            {
                if (userInitiated)
                {
                    MessageBox.Show(this, "\u6682\u65f6\u6ca1\u6709\u627e\u5230\u53ef\u7528\u66f4\u65b0\u3002", "\u68c0\u67e5\u66f4\u65b0", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus("\u672a\u627e\u5230\u53ef\u7528\u66f4\u65b0");
                }

                return;
            }

            if (!UpdateService.IsNewer(updateInfo))
            {
                if (userInitiated)
                {
                    MessageBox.Show(this, $"\u5f53\u524d\u5df2\u662f\u6700\u65b0\u7248\u672c\u3002\n\n\u5f53\u524d\uff1a{AppInfo.Version}\n\u6700\u65b0\uff1a{updateInfo.Version}", "\u68c0\u67e5\u66f4\u65b0", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus($"\u5f53\u524d\u5df2\u662f\u6700\u65b0\u7248\u672c\uff1a{AppInfo.Version}");
                }

                return;
            }

            var result = MessageBox.Show(
                this,
                $"\u53d1\u73b0\u65b0\u7248\u672c {updateInfo.Version}\uff0c\u5f53\u524d\u7248\u672c {AppInfo.Version}\u3002\n\n\u662f\u5426\u4e0b\u8f7d\u5e76\u542f\u52a8\u66f4\u65b0\uff1f",
                "\u53d1\u73b0\u65b0\u7248\u672c",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (result != DialogResult.Yes)
            {
                return;
            }

            SetStatus($"\u6b63\u5728\u4e0b\u8f7d\u66f4\u65b0 {updateInfo.Version}...");
            var installer = await _updateService.DownloadInstallerAsync(updateInfo);
            UpdateService.LaunchInstaller(installer);
            LogInfo($"\u5df2\u542f\u52a8\u66f4\u65b0\u7a0b\u5e8f\uff1a{updateInfo.Version}");
            _allowClose = true;
            Close();
        }
        catch (Exception ex)
        {
            LogError("\u68c0\u67e5\u6216\u542f\u52a8\u66f4\u65b0\u5931\u8d25", ex);
            if (userInitiated)
            {
                MessageBox.Show(this, $"\u68c0\u67e5\u6216\u542f\u52a8\u66f4\u65b0\u5931\u8d25\uff1a{ex.Message}", "\u66f4\u65b0\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            _checkUpdateButton.Enabled = true;
        }
    }

    private void UpdateActionButtons()
    {
        _previewButton.Text = _previewing ? "\u505c\u6b62\u9884\u89c8" : "\u5171\u4eab\u9884\u89c8";
        _monitorButton.Text = _monitoring ? "\u505c\u6b62\u76d1\u63a7" : "\u5f00\u59cb\u76d1\u63a7";
        StyleButton(_monitorButton, _monitoring ? Danger : Accent, Color.White);
        _statePill.Text = _monitoring ? "\u76d1\u63a7\u4e2d" : _previewing ? "\u9884\u89c8\u4e2d" : "\u5c31\u7eea";
        _statePill.BackColor = _monitoring ? Accent : _previewing ? Color.FromArgb(55, 105, 150) : Color.FromArgb(66, 78, 92);
    }

    private void UpdateOcrModeVisibility()
    {
        var ocrEngineEnabled = _ocrEnabled.Checked;
        var commandMode = string.Equals(_ocrMode.SelectedItem?.ToString(), "command", StringComparison.OrdinalIgnoreCase);
        var wxMode = string.Equals(_ocrMode.SelectedItem?.ToString(), "wxocr", StringComparison.OrdinalIgnoreCase);
        _ocrMode.Enabled = ocrEngineEnabled;
        _ocrUrl.Enabled = ocrEngineEnabled && wxMode;
        _minConfidenceInput.Enabled = ocrEngineEnabled && wxMode;
        _ocrCommand.Enabled = ocrEngineEnabled && commandMode;
        _ocrArguments.Enabled = ocrEngineEnabled && commandMode;
        _maxOcrPixelsInput.Enabled = ocrEngineEnabled;
    }

    private void UpdateDetectionModeVisibility()
    {
        var ocrMode = _ocrKeywordEnabled.Checked;
        foreach (var control in _sourceSelectionControls.Concat(_regionSelectionControls).Concat(_ocrSourceControls))
        {
            control.Visible = ocrMode;
        }

        if (!ocrMode)
        {
            _previewing = false;
            _preview.Image = null;
            _previewEmptyLabel.Visible = true;
        }

        if (_monitorPreviewArea is not null)
        {
            _monitorPreviewArea.Visible = ocrMode;
        }

        LayoutMonitorSplit();

        _navOcrButton.Enabled = true;
        _ocrEnabled.Enabled = true;
        _ocrText.PlaceholderText = ocrMode
            ? "\u6700\u8fd1\u4e00\u6b21 OCR \u6587\u672c\u4f1a\u663e\u793a\u5728\u8fd9\u91cc"
            : "\u542f\u7528 OCR \u753b\u9762\u76d1\u63a7\u540e\u4f1a\u663e\u793a\u8bc6\u522b\u7ed3\u679c";
        UpdateOcrModeVisibility();
    }

    private void ResetOcrResultViews()
    {
        if (_loadingConfig)
        {
            return;
        }

        _ocrText.Clear();
        _keywordLastHitText.Clear();
        _ocrText.PlaceholderText = _ocrKeywordEnabled.Checked
            ? "\u6700\u8fd1\u4e00\u6b21 OCR \u6587\u672c\u4f1a\u663e\u793a\u5728\u8fd9\u91cc"
            : "\u542f\u7528 OCR \u753b\u9762\u76d1\u63a7\u540e\u4f1a\u663e\u793a\u8bc6\u522b\u7ed3\u679c";
        _keywordLastHitText.PlaceholderText = "\u5173\u952e\u8bcd\u547d\u4e2d\u548c\u76d1\u63a7\u533a\u57df\u4f1a\u663e\u793a\u5728\u8fd9\u91cc";
    }

    private void LayoutMonitorSplit()
    {
        if (_monitorOcrLayout is null || _monitorPreviewArea is null || _monitorOcrLayout.Width <= 0)
        {
            LayoutMonitorTaskCards();
            return;
        }

        var previewMode = _monitorPreviewArea.Visible;
        _monitorOcrLayout.SuspendLayout();
        _monitorOcrLayout.ColumnStyles[0].SizeType = previewMode ? SizeType.Absolute : SizeType.Percent;
        _monitorOcrLayout.ColumnStyles[0].Width = previewMode
            ? Math.Min(680, Math.Max(560, (int)(_monitorOcrLayout.Width * 0.46)))
            : 100;
        _monitorOcrLayout.ColumnStyles[1].SizeType = previewMode ? SizeType.Percent : SizeType.Absolute;
        _monitorOcrLayout.ColumnStyles[1].Width = previewMode ? 100 : 0;
        _monitorOcrLayout.ResumeLayout();
        LayoutMonitorTaskCards();
    }

    private void LayoutMonitorTaskCards()
    {
        if (_monitorTasksHost is null)
        {
            return;
        }

        var previewMode = _monitorPreviewArea is not null && _monitorPreviewArea.Visible;
        _monitorTasksHost.FlowDirection = previewMode ? FlowDirection.TopDown : FlowDirection.LeftToRight;
        _monitorTasksHost.WrapContents = !previewMode;
        foreach (Control control in _monitorTasksHost.Controls)
        {
            control.AutoSize = false;
            control.Width = previewMode
                ? Math.Max(460, _monitorTasksHost.ClientSize.Width - 28)
                : Math.Max(720, _monitorTasksHost.ClientSize.Width - 28);
            control.Height = Math.Max(control.PreferredSize.Height, 138);
        }
    }

    private static void LayoutDashboardCards(FlowLayoutPanel page)
    {
        if (string.Equals(page.Tag?.ToString(), "single", StringComparison.OrdinalIgnoreCase))
        {
            foreach (Control control in page.Controls)
            {
                control.Width = Math.Max(560, page.ClientSize.Width - 28);
            }

            return;
        }

        var twoColumnWidth = Math.Max(420, (page.ClientSize.Width - 48) / 2);
        foreach (Control control in page.Controls)
        {
            control.Width = Math.Min(680, twoColumnWidth);
        }
    }

    private void ApplyTimerInterval()
    {
        _timer.Interval = Math.Max(100, (int)_intervalInput.Value);
    }

    private void UpdateRegionLabel()
    {
        var region = _preview.SelectedRegion;
        _regionLabel.Text = region.IsEmpty
            ? "\u672a\u6846\u9009\uff0c\u5c06\u76d1\u63a7\u6240\u9009\u6765\u6e90\u7684\u5168\u90e8\u753b\u9762"
            : $"x={region.X}, y={region.Y}, w={region.Width}, h={region.Height}";
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = $"{DateTime.Now:HH:mm:ss}  {message}";
    }

    private static Control CreateCardPage()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            Padding = new Padding(16)
        };
    }

    private FlowLayoutPanel CreateDashboardPage()
    {
        var page = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = PageBack,
            Padding = new Padding(0, 0, 10, 0)
        };
        page.SizeChanged += (_, _) => LayoutDashboardCards(page);
        return page;
    }

    private TableLayoutPanel CreateTaskCard(string title, string description)
    {
        var layout = new TableLayoutPanel
        {
            Width = 560,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = PanelBack,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = Color.FromArgb(28, 38, 50),
            Font = new Font(Font.FontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 0, 0, 4)
        };
        layout.Controls.Add(titleLabel, 0, layout.RowCount++);

        var descriptionLabel = new Label
        {
            Text = description,
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = MutedText,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.Controls.Add(descriptionLabel, 0, layout.RowCount++);

        var form = CreateFormPanel();
        form.Dock = DockStyle.Top;
        form.AutoSize = true;
        form.AutoScroll = false;
        form.Padding = new Padding(0);
        layout.Controls.Add(form, 0, layout.RowCount++);
        form.Tag = layout;
        return form;
    }

    private static Control GetTaskCardContainer(TableLayoutPanel form)
    {
        return form.Tag as Control ?? form;
    }

    private static void SetTaskCardPresentation(TableLayoutPanel form, int width)
    {
        if (width <= 0)
        {
            form.Dock = DockStyle.Fill;
            form.AutoSize = false;
        }

        if (GetTaskCardContainer(form) is TableLayoutPanel container)
        {
            container.Dock = width <= 0 ? DockStyle.Fill : DockStyle.None;
            container.AutoSize = false;
            container.Margin = new Padding(0);
            container.MaximumSize = width <= 0 ? Size.Empty : new Size(width, 0);
            if (width > 0)
            {
                container.Width = width;
            }
        }
    }

    private static TableLayoutPanel CreateFormPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 2,
            BackColor = PanelBack,
            Padding = new Padding(4)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return panel;
    }

    private static Label AddSection(TableLayoutPanel panel, string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.FromArgb(34, 45, 58),
            Font = new Font(SystemFonts.DefaultFont.FontFamily, SystemFonts.DefaultFont.Size + 1, FontStyle.Bold),
            Margin = new Padding(0, 14, 0, 8)
        };
        panel.Controls.Add(label, 0, panel.RowCount);
        panel.SetColumnSpan(label, 2);
        panel.RowCount++;
        return label;
    }

    private static void AddFull(TableLayoutPanel panel, Control control)
    {
        control.Dock = DockStyle.None;
        control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        control.Margin = new Padding(0, 0, 0, 10);
        if (control is TextBox or ComboBox or NumericUpDown)
        {
            control.MinimumSize = new Size(240, control.MinimumSize.Height);
            control.Width = Math.Min(560, Math.Max(360, control.Width));
        }

        panel.Controls.Add(control, 0, panel.RowCount);
        panel.SetColumnSpan(control, 2);
        panel.RowCount++;
    }

    private static Label AddLabeled(TableLayoutPanel panel, string labelText, Control control)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = MutedText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 3, 8, 10)
        };
        control.Dock = DockStyle.None;
        control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        control.Margin = new Padding(0, 0, 0, 10);
        control.MinimumSize = new Size(240, control.MinimumSize.Height);
        if (control is TextBox or ComboBox or NumericUpDown)
        {
            control.Width = Math.Min(560, Math.Max(360, control.Width));
        }
        panel.Controls.Add(label, 0, panel.RowCount);
        panel.Controls.Add(control, 1, panel.RowCount);
        panel.RowCount++;
        return label;
    }

    private static Label AddNumber(TableLayoutPanel panel, string labelText, NumericUpDown input, int min, int max)
    {
        input.Minimum = min;
        input.Maximum = max;

        return AddLabeled(panel, labelText, input);
    }

    private static void ConfigureBodyTemplateTextBox(TextBox textBox, string placeholder)
    {
        textBox.Multiline = true;
        textBox.ScrollBars = ScrollBars.Both;
        textBox.WordWrap = false;
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(6);
        textBox.PlaceholderText = placeholder;
    }

    private static void AddBodyTemplateTab(TabControl tabs, string title, TextBox textBox)
    {
        var page = new TabPage(title) { Padding = new Padding(6) };
        page.Controls.Add(textBox);
        tabs.TabPages.Add(page);
    }

    private static void StretchFormInputs(TableLayoutPanel panel)
    {
        foreach (Control control in panel.Controls)
        {
            if (control is TextBox or ComboBox or NumericUpDown or CheckedListBox or TabControl)
            {
                control.Dock = DockStyle.Top;
                control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                control.MaximumSize = Size.Empty;
            }
        }
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.Height = 40;
        button.Width = Math.Max(button.Width, 128);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 8, 8);
    }

    private void AddNavButton(FlowLayoutPanel nav, CenteredTabLabel button, string text, Action onClick)
    {
        button.Text = text;
        button.Width = 118;
        button.Height = 38;
        button.Margin = new Padding(0, 0, 10, 0);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.ForeColor = Color.FromArgb(54, 67, 84);
        button.BackColor = Color.FromArgb(238, 242, 247);
        button.Font = new Font(Font.FontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
        button.Cursor = Cursors.Hand;
        button.Click += (_, _) => onClick();
        nav.Controls.Add(button);
    }

    private void AddSubTabButton(FlowLayoutPanel nav, CenteredTabLabel button, string text, Action onClick)
    {
        button.Text = text;
        button.Width = 148;
        button.Height = 36;
        button.Margin = new Padding(0, 0, 10, 0);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.ForeColor = Color.FromArgb(54, 67, 84);
        button.BackColor = Color.FromArgb(238, 242, 247);
        button.Font = new Font(Font.FontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
        button.Cursor = Cursors.Hand;
        button.Click += (_, _) => onClick();
        nav.Controls.Add(button);
    }

    private void AddMonitorSubPage(Control page)
    {
        page.Visible = false;
        page.Dock = DockStyle.Fill;
        _monitorSubPages.Add(page);
        _monitorSubPageHost.Controls.Add(page);
    }

    private void ShowMonitorSubPage(int index)
    {
        for (var i = 0; i < _monitorSubPages.Count; i++)
        {
            _monitorSubPages[i].Visible = i == index;
        }

        if (index >= 0 && index < _monitorSubPages.Count)
        {
            _monitorSubPages[index].BringToFront();
        }

        var buttons = new[] { _monitorOcrTaskButton, _monitorDialogTaskButton, _monitorTaskbarTaskButton };
        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].BackColor = i == index ? Accent : Color.FromArgb(238, 242, 247);
            buttons[i].ForeColor = i == index ? Color.White : Color.FromArgb(54, 67, 84);
        }

        LayoutMonitorSplit();
    }

    private void ShowSettingsPage(int index)
    {
        for (var i = 0; i < _settingPages.Count; i++)
        {
            _settingPages[i].Visible = i == index;
        }

        if (index >= 0 && index < _settingPages.Count)
        {
            _settingPages[index].BringToFront();
        }

        var buttons = new[] { _navMonitorButton, _navOcrButton, _navWebhookButton, _navLogButton };
        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].BackColor = i == index ? Accent : Color.FromArgb(238, 242, 247);
            buttons[i].ForeColor = i == index ? Color.White : Color.FromArgb(54, 67, 84);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            var result = MessageBox.Show(
                "\u8981\u9000\u51fa\u7a0b\u5e8f\u5417\uff1f\n\n\u662f\uff1a\u9000\u51fa\n\u5426\uff1a\u6700\u5c0f\u5316\n\u53d6\u6d88\uff1a\u7559\u5728\u5f53\u524d\u7a97\u53e3",
                "\u5173\u95ed\u786e\u8ba4",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                SetStatus("\u5df2\u6700\u5c0f\u5316\uff0c\u76d1\u63a7\u4f1a\u7ee7\u7eed\u8fd0\u884c");
                return;
            }

            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            _allowClose = true;
        }

        _closing = true;
        _monitoring = false;
        _previewing = false;
        _timer.Stop();
        _preview.Image = null;
        _previewEmptyLabel.Visible = true;
        SaveConfigFromUi();
        _wxOcrService.Dispose();
        _localOcrService.Dispose();
        _updateService.Dispose();
        _notificationService.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _shellHookMessage = Win32Window.RegisterShellHookMessage();
        _shellHookRegistered = _shellHookMessage != 0 && Win32Window.RegisterShellHook(Handle);
        if (!_shellHookRegistered)
        {
            LogWarn("\u4efb\u52a1\u680f\u95ea\u70c1\u76d1\u542c\u6ce8\u518c\u5931\u8d25\uff0c\u8be5\u529f\u80fd\u53ef\u80fd\u4e0d\u53ef\u7528");
        }
        else
        {
            LogInfo("\u4efb\u52a1\u680f\u95ea\u70c1\u76d1\u542c\u5df2\u6ce8\u518c");
        }

        _alertWinEventHook = Win32Window.RegisterAlertWinEventHook(_alertWinEventCallback);
        if (_alertWinEventHook == IntPtr.Zero)
        {
            LogWarn("\u4efb\u52a1\u680f\u95ea\u70c1\u5907\u7528\u76d1\u542c\u6ce8\u518c\u5931\u8d25");
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_shellHookRegistered)
        {
            Win32Window.DeregisterShellHook(Handle);
            _shellHookRegistered = false;
        }

        if (_alertWinEventHook != IntPtr.Zero)
        {
            Win32Window.UnregisterWinEventHook(_alertWinEventHook);
            _alertWinEventHook = IntPtr.Zero;
        }

        base.OnHandleDestroyed(e);
    }

    private void OnAlertWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (eventType != Win32Window.EventSystemAlert || hwnd == IntPtr.Zero || IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(new Action(() => HandleTaskbarFlash(hwnd)));
        }
        catch (InvalidOperationException)
        {
            // Window is closing.
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (_shellHookMessage != 0 &&
            m.Msg == _shellHookMessage &&
            Win32Window.IsShellFlashEvent(m.WParam))
        {
            HandleTaskbarFlash(m.LParam);
        }

        base.WndProc(ref m);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            _preview.Image = null;
            _previewEmptyLabel.Visible = true;
        }
    }
}
