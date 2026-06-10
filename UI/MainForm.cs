using MhxyNotify.Models;
using MhxyNotify.Native;
using MhxyNotify.Services;

namespace MhxyNotify.UI;

public sealed class MainForm : Form
{
    private const int SettingsPanelWidth = 560;
    private const int MaxUnchangedKeywordNotifications = 3;

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
    private readonly Button _refreshButton = new();
    private readonly Button _previewButton = new();
    private readonly Button _monitorButton = new();
    private readonly PreviewCanvas _preview = new();
    private readonly ComboBox _keywordDetectionMode = new();
    private readonly NumericUpDown _intervalInput = new();
    private readonly NumericUpDown _cooldownInput = new();

    private readonly CheckBox _ocrEnabled = new();
    private readonly ComboBox _ocrMode = new();
    private readonly TextBox _ocrUrl = new();
    private readonly TextBox _ocrCommand = new();
    private readonly TextBox _ocrArguments = new();
    private readonly NumericUpDown _minConfidenceInput = new();
    private readonly NumericUpDown _maxOcrPixelsInput = new();
    private readonly TextBox _ocrText = new();
    private readonly NumericUpDown _keywordOcrIntervalInput = new();
    private readonly TextBox _watchKeywords = new();
    private readonly TextBox _keywordLastHitText = new();

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
    private readonly List<NotificationChannelConfig> _notificationChannels = [];

    private readonly Label _regionLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _statePill = new();
    private readonly Button _checkUpdateButton = new();
    private readonly TextBox _logViewer = new();
    private readonly NumericUpDown _maxLogLinesInput = new();
    private readonly Button _refreshLogButton = new();
    private readonly Button _clearLogButton = new();
    private readonly System.Windows.Forms.Panel _pageHost = new();
    private readonly List<Control> _settingPages = [];
    private readonly List<Control> _sourceSelectionControls = [];
    private readonly List<Control> _regionSelectionControls = [];
    private readonly List<Control> _ocrSourceControls = [];
    private readonly Button _navMonitorButton = new();
    private readonly Button _navOcrButton = new();
    private readonly Button _navWebhookButton = new();
    private readonly Button _navLogButton = new();

    private bool _previewing;
    private bool _monitoring;
    private bool _polling;
    private bool _allowClose;
    private bool _closing;
    private bool _loadingConfig;
    private bool _updatingWebhookChannels;
    private WindowInfo? _selectedWindow;
    private string _lastKeywordHit = "";
    private string _lastKeywordSignature = "";
    private int _unchangedKeywordNotifyCount;
    private DateTimeOffset _lastKeywordOcrAt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastKeywordNotifyAt = DateTimeOffset.MinValue;

    public MainForm()
    {
        _config = AppConfig.Load();
        _notificationService = new NotificationService(_logService);
        _updateService = new UpdateService(_logService);
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
            RowCount = 2,
            ColumnCount = 1,
            BackColor = PageBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = PageBack,
            Padding = new Padding(12)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SettingsPanelWidth));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(content, 0, 1);

        var settingsTabs = BuildSettingsTabs();
        settingsTabs.Margin = new Padding(0, 0, 12, 0);
        content.Controls.Add(settingsTabs, 0, 0);
        content.Controls.Add(BuildPreviewArea(), 1, 0);
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
        _statePill.Size = new Size(132, 42);
        _statePill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        header.Controls.Add(_statePill);

        _checkUpdateButton.Text = "\u68c0\u67e5\u66f4\u65b0";
        StyleButton(_checkUpdateButton, Color.FromArgb(66, 78, 92), Color.White);
        _checkUpdateButton.Size = new Size(148, 42);
        _checkUpdateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _checkUpdateButton.Click += async (_, _) => await CheckForUpdatesAsync(userInitiated: true);
        header.Controls.Add(_checkUpdateButton);

        _monitorButton.Text = "\u5f00\u59cb\u76d1\u63a7";
        StyleButton(_monitorButton, Accent, Color.White);
        _monitorButton.Size = new Size(180, 42);
        _monitorButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
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
        header.HandleCreated += (_, _) => LayoutHeader();

        return header;
    }

    private Control BuildSettingsTabs()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = PageBack
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var nav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(31, 42, 56),
            Padding = new Padding(10)
        };
        var navStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
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
        _pageHost.Padding = new Padding(12, 0, 0, 0);
        _pageHost.MinimumSize = new Size(SettingsPanelWidth - 104, 0);

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
        shell.Controls.Add(_pageHost, 1, 0);
        ShowSettingsPage(0);
        return shell;
    }

    private Control BuildMonitorPage()
    {
        var page = CreateCardPage();
        var panel = CreateFormPanel();
        page.Controls.Add(panel);

        var windowSection = AddSection(panel, "\u7a97\u53e3");
        _sourceSelectionControls.Add(windowSection);
        _windowList.DropDownStyle = ComboBoxStyle.DropDownList;
        AddFull(panel, _windowList);
        _sourceSelectionControls.Add(_windowList);

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _refreshButton.Text = "\u5237\u65b0\u7a97\u53e3";
        _refreshButton.Click += (_, _) => RefreshWindows();
        StyleButton(_refreshButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        buttonRow.Controls.Add(_refreshButton);

        _previewButton.Text = "\u5171\u4eab\u9884\u89c8";
        _previewButton.Click += (_, _) => TogglePreview();
        StyleButton(_previewButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        buttonRow.Controls.Add(_previewButton);
        AddFull(panel, buttonRow);
        _sourceSelectionControls.Add(buttonRow);

        AddSection(panel, "\u5173\u952e\u8bcd\u76d1\u63a7");
        _keywordDetectionMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _keywordDetectionMode.Items.Clear();
        _keywordDetectionMode.Items.AddRange(["OCR \u56fe\u50cf\u8bc6\u522b", "\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c"]);
        _keywordDetectionMode.SelectedIndexChanged += (_, _) =>
        {
            UpdateDetectionModeVisibility();
            SaveConfigFromUi();
        };
        AddLabeled(panel, "识别方式", _keywordDetectionMode);

        var previewIntervalLabel = AddNumber(panel, "\u9884\u89c8\u95f4\u9694\u6beb\u79d2", _intervalInput, 100, 5000);
        _ocrSourceControls.Add(previewIntervalLabel);
        _ocrSourceControls.Add(_intervalInput);
        AddNumber(panel, "识别间隔毫秒", _keywordOcrIntervalInput, 500, 30000);
        AddNumber(panel, "\u51b7\u5374\u79d2\u6570", _cooldownInput, 0, 3600);

        _watchKeywords.Multiline = true;
        _watchKeywords.ScrollBars = ScrollBars.Vertical;
        _watchKeywords.Height = 104;
        _watchKeywords.PlaceholderText = "\u7f51\u7edc\u9519\u8bef\r\n\u8bf7\u91cd\u65b0\u767b\u5f55\r\nxxx";
        AddLabeled(panel, "\u547d\u4e2d\u5173\u952e\u8bcd", _watchKeywords);

        var keywordHint = new Label
        {
            AutoSize = true,
            ForeColor = MutedText,
            Text = "\u6bcf\u884c\u4e00\u4e2a\u5173\u952e\u8bcd\uff1b\u542f\u52a8\u76d1\u63a7\u524d\u5fc5\u987b\u81f3\u5c11\u586b\u4e00\u4e2a\u3002\u547d\u4e2d\u4efb\u610f\u4e00\u4e2a\u5c31\u53d1\u901a\u77e5\u3002"
        };
        AddFull(panel, keywordHint);

        var regionSection = AddSection(panel, "\u76d1\u63a7\u533a\u57df");
        _regionSelectionControls.Add(regionSection);
        _regionLabel.AutoSize = true;
        _regionLabel.ForeColor = MutedText;
        _regionLabel.Text = "\u672a\u6846\u9009\uff0c\u5c06\u76d1\u63a7\u6240\u9009\u6765\u6e90\u7684\u5168\u90e8\u753b\u9762";
        AddFull(panel, _regionLabel);
        _regionSelectionControls.Add(_regionLabel);
        var regionHint = new Label
        {
            AutoSize = true,
            ForeColor = MutedText,
            Text = "\u5148\u9009\u62e9\u6765\u6e90\uff1a\u53ef\u4ee5\u662f\u7a97\u53e3\u3001\u6574\u4e2a\u684c\u9762\u6216\u67d0\u4e2a\u663e\u793a\u5668\u3002\u53f3\u4fa7\u9884\u89c8\u53ef\u9009\u62e9\u6027\u6846\u9009\uff1b\u4e0d\u6846\u9009\u5c31 OCR \u6574\u4e2a\u6240\u9009\u6765\u6e90\u3002"
        };
        AddFull(panel, regionHint);
        _regionSelectionControls.Add(regionHint);

        return page;
    }

    private Control BuildLogPage()
    {
        var page = CreateCardPage();
        var panel = CreateFormPanel();
        page.Controls.Add(panel);

        AddSection(panel, "\u65e5\u5fd7\u63a7\u5236");
        AddNumber(panel, "\u6700\u5927\u884c\u6570", _maxLogLinesInput, 100, 50000);

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _refreshLogButton.Text = "\u5237\u65b0\u65e5\u5fd7";
        _refreshLogButton.Click += (_, _) => RefreshLogViewer();
        StyleButton(_refreshLogButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        buttonRow.Controls.Add(_refreshLogButton);

        _clearLogButton.Text = "\u6e05\u7a7a\u65e5\u5fd7";
        _clearLogButton.Click += (_, _) => ClearLogs();
        StyleButton(_clearLogButton, Danger, Color.White);
        buttonRow.Controls.Add(_clearLogButton);
        AddFull(panel, buttonRow);

        AddSection(panel, "\u65e5\u5fd7\u5185\u5bb9");
        _logViewer.Multiline = true;
        _logViewer.ReadOnly = true;
        _logViewer.ScrollBars = ScrollBars.Both;
        _logViewer.WordWrap = false;
        _logViewer.Height = 420;
        AddFull(panel, _logViewer);

        return page;
    }

    private Control BuildOcrPage()
    {
        var page = CreateCardPage();
        var panel = CreateFormPanel();
        page.Controls.Add(panel);

        AddSection(panel, "OCR");
        _ocrEnabled.Text = "\u542f\u7528 OCR";
        AddFull(panel, _ocrEnabled);

        _ocrMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _ocrMode.Items.AddRange(["wxocr", "local", "command"]);
        _ocrMode.SelectedIndexChanged += (_, _) => UpdateOcrModeVisibility();
        AddLabeled(panel, "\u6a21\u5f0f", _ocrMode);

        _ocrUrl.PlaceholderText = "http://192.168.88.3:5000/ocr";
        AddLabeled(panel, "wxocr URL", _ocrUrl);

        _ocrCommand.PlaceholderText = "tesseract";
        AddLabeled(panel, "\u672c\u5730\u547d\u4ee4", _ocrCommand);

        _ocrArguments.PlaceholderText = "\"{image}\" stdout -l chi_sim+eng --psm 6";
        AddLabeled(panel, "\u547d\u4ee4\u53c2\u6570", _ocrArguments);

        AddNumber(panel, "\u6700\u4f4e\u7f6e\u4fe1", _minConfidenceInput, 0, 100);
        AddNumber(panel, "OCR \u6700\u5927\u50cf\u7d20\uff08\u4e07\uff09", _maxOcrPixelsInput, 20, 500);

        AddSection(panel, "\u6700\u8fd1 OCR \u7ed3\u679c");
        _ocrText.Multiline = true;
        _ocrText.ReadOnly = true;
        _ocrText.ScrollBars = ScrollBars.Vertical;
        _ocrText.Height = 200;
        _ocrText.PlaceholderText = "\u6700\u8fd1\u4e00\u6b21 OCR \u6587\u672c\u4f1a\u663e\u793a\u5728\u8fd9\u91cc";
        AddFull(panel, _ocrText);

        AddSection(panel, "\u5173\u952e\u8bcd\u547d\u4e2d");
        _keywordLastHitText.Multiline = true;
        _keywordLastHitText.ReadOnly = true;
        _keywordLastHitText.ScrollBars = ScrollBars.Vertical;
        _keywordLastHitText.Height = 120;
        _keywordLastHitText.PlaceholderText = "\u5173\u952e\u8bcd\u547d\u4e2d\u548c\u76d1\u63a7\u533a\u57df\u4f1a\u663e\u793a\u5728\u8fd9\u91cc";
        AddFull(panel, _keywordLastHitText);

        return page;
    }

    private Control BuildWebhookPage()
    {
        var page = CreateCardPage();
        var panel = CreateFormPanel();
        page.Controls.Add(panel);

        AddSection(panel, "\u901a\u77e5\u6e20\u9053");
        _webhookChannels.CheckOnClick = true;
        _webhookChannels.Height = 118;
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
        AddFull(panel, _webhookChannels);

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
        AddFull(panel, channelButtons);

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
        AddFull(panel, _webhookEnabled);

        _webhookName.PlaceholderText = "\u4f8b\u5982\uff1a\u9489\u9489\u7fa4\u3001Bark\u624b\u673a";
        AddLabeled(panel, "\u6e20\u9053\u540d", _webhookName);

        _webhookPreset.DropDownStyle = ComboBoxStyle.DropDownList;
        _webhookPreset.Items.AddRange(["generic", "serverchan", "bark", "dingtalk", "feishu", "wecom", "custom"]);
        AddLabeled(panel, "\u9884\u8bbe", _webhookPreset);

        _applyWebhookPresetButton.Text = "\u5e94\u7528\u9884\u8bbe";
        _applyWebhookPresetButton.Click += (_, _) => ApplyWebhookPreset(_webhookPreset.SelectedItem?.ToString() ?? "generic");
        StyleButton(_applyWebhookPresetButton, Color.FromArgb(235, 239, 244), Color.FromArgb(38, 48, 60));
        AddFull(panel, _applyWebhookPresetButton);

        _webhookMethod.PlaceholderText = "POST";
        AddLabeled(panel, "Method", _webhookMethod);

        _webhookUrl.PlaceholderText = "https://example.com/webhook";
        AddLabeled(panel, "URL", _webhookUrl);

        _webhookHeaders.Multiline = true;
        _webhookHeaders.ScrollBars = ScrollBars.Vertical;
        _webhookHeaders.Height = 86;
        _webhookHeaders.PlaceholderText = "Content-Type: application/json";
        AddLabeled(panel, "Headers", _webhookHeaders);

        _webhookBodyTemplate.Multiline = true;
        _webhookBodyTemplate.ScrollBars = ScrollBars.Vertical;
        _webhookBodyTemplate.Height = 190;
        _webhookBodyTemplate.PlaceholderText = "{\"title\":\"{title}\",\"body\":\"{body}\"}";
        AddLabeled(panel, "Body \u6a21\u677f", _webhookBodyTemplate);

        var hint = new Label
        {
            AutoSize = true,
            ForeColor = MutedText,
            Text = "\u53ef\u7528\u53d8\u91cf\uff1a{title} {body} {window} {distance} {region} {ocrText} {time} {timestamp}\nJSON \u5916\u53ef\u7528\uff1a{bodyRaw} {bodyUrl} {titleRaw} {titleUrl}"
        };
        AddFull(panel, hint);

        return page;
    }

    private Control BuildPreviewArea()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = PageBack,
            Padding = new Padding(12, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var previewHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 24, 32),
            Padding = new Padding(1)
        };
        _preview.Dock = DockStyle.Fill;
        _preview.SelectionChanged += (_, _) =>
        {
            UpdateRegionLabel();
            SaveConfigFromUi();
        };
        previewHost.Controls.Add(_preview);
        panel.Controls.Add(previewHost, 0, 0);

        var statusPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            Padding = new Padding(14, 0, 14, 0)
        };
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = MutedText;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Text = "\u5c31\u7eea";
        statusPanel.Controls.Add(_statusLabel);
        panel.Controls.Add(statusPanel, 0, 1);

        return panel;
    }

    private void LoadConfigIntoUi()
    {
        _loadingConfig = true;
        _intervalInput.Value = Math.Clamp(_config.PollIntervalMs, 100, 5000);
        _cooldownInput.Value = Math.Clamp(_config.CooldownSeconds, 0, 3600);
        _keywordDetectionMode.SelectedItem = ResolveKeywordDetectionModeLabel(_config.KeywordDetectionMode);

        _ocrEnabled.Checked = _config.OcrEnabled;
        _ocrMode.SelectedItem = ResolveOcrMode(_config.OcrMode);
        _ocrUrl.Text = _config.OcrUrl;
        _ocrCommand.Text = _config.OcrCommand;
        _ocrArguments.Text = _config.OcrArguments;
        _minConfidenceInput.Value = (decimal)Math.Clamp(_config.MinConfidence * 100, 0, 100);
        var maxOcrPixels = _config.MaxOcrPixels == 2_000_000 ? 800_000 : _config.MaxOcrPixels;
        _maxOcrPixelsInput.Value = Math.Clamp(maxOcrPixels / 10_000, 20, 500);
        _keywordOcrIntervalInput.Value = Math.Clamp(_config.KeywordOcrIntervalMs, 500, 30000);
        _watchKeywords.Text = _config.WatchKeywords;

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

        var windowCount = windows.Count(window => !window.IsDesktopSource);
        var desktopSourceCount = windows.Count(window => window.IsDesktopSource);
        SetStatus($"\u5df2\u627e\u5230 {windowCount} \u4e2a\u7a97\u53e3\uff0c{desktopSourceCount} \u4e2a\u684c\u9762/\u663e\u793a\u5668\u6765\u6e90");
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
            SetStatus("\u9884\u89c8\u5df2\u505c\u6b62");
            UpdateActionButtons();
            return;
        }

        var ocrKeywordMode = IsOcrKeywordDetectionMode();
        if (ocrKeywordMode && !TrySelectWindow())
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

        var ocrKeywordMode = IsOcrKeywordDetectionMode();
        if (ocrKeywordMode && !TrySelectWindow())
        {
            return;
        }

        if (ocrKeywordMode && !ValidateOcrSettingsForMonitor())
        {
            return;
        }

        if (ocrKeywordMode && !_ocrEnabled.Checked)
        {
            MessageBox.Show(this, "\u5173\u952e\u8bcd\u76d1\u63a7\u9700\u8981\u5f00\u542f OCR\u3002", "\u9700\u8981 OCR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (GetWatchKeywords().Count == 0)
        {
            MessageBox.Show(this, "\u8bf7\u81f3\u5c11\u8f93\u5165\u4e00\u4e2a\u547d\u4e2d\u5173\u952e\u8bcd\u540e\u518d\u5f00\u59cb\u76d1\u63a7\u3002", "\u9700\u8981\u5173\u952e\u8bcd", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaveConfigFromUi();
        _monitoring = true;
        _previewing = ocrKeywordMode;
        if (!ocrKeywordMode)
        {
            _preview.Image = null;
        }

        _lastKeywordHit = "";
        _lastKeywordSignature = "";
        _unchangedKeywordNotifyCount = 0;
        _lastKeywordOcrAt = DateTimeOffset.MinValue;
        _lastKeywordNotifyAt = DateTimeOffset.MinValue;
        ResetOcrResultViews();
        ApplyTimerInterval();
        _timer.Start();
        SetStatus(ocrKeywordMode
            ? $"\u5173\u952e\u8bcd OCR \u76d1\u63a7\u5df2\u542f\u52a8\uff1a{GetSelectedSourceTitle()}\uff1b{GetSelectionModeText()}"
            : "\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c\u76d1\u63a7\u5df2\u542f\u52a8\uff1a\u5168\u5c40\u5916\u90e8\u7a97\u53e3");
        UpdateActionButtons();
        if (!ocrKeywordMode)
        {
            SetStatus("\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c\u76d1\u63a7\u5df2\u542f\u52a8\uff1a\u5168\u5c40\u5916\u90e8\u7a97\u53e3");
        }

        await Task.CompletedTask;
    }

    private async void TimerOnTick(object? sender, EventArgs e)
    {
        if (_closing || _polling || (IsOcrKeywordDetectionMode() && _selectedWindow is null))
        {
            return;
        }

        var needsPreview = ShouldRenderPreviewFrame() && IsOcrKeywordDetectionMode();
        var needsKeywordCheck = _monitoring && ShouldRunKeywordCheck();
        if (!needsPreview && !needsKeywordCheck)
        {
            return;
        }

        _polling = true;
        try
        {
            if (needsKeywordCheck && IsWindowTextKeywordDetectionMode())
            {
                if (!await TryNotifyDialogKeywordAsync())
                {
                    SetStatus("\u5916\u90e8\u7a97\u53e3/\u5f39\u7a97\u6587\u672c\u76d1\u63a7\u4e2d\uff0c\u672a\u547d\u4e2d");
                }

                if (!needsPreview)
                {
                    return;
                }
            }

            var needsOcr = needsKeywordCheck && IsOcrKeywordDetectionMode();
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
            }
            else if (WindowState == FormWindowState.Minimized && _preview.Image is not null)
            {
                _preview.Image = null;
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
        var source = IsOcrKeywordDetectionMode() ? _selectedWindow : null;
        if (IsOcrKeywordDetectionMode() && source is null)
        {
            return false;
        }

        var keywords = GetWatchKeywords();
        if (keywords.Count == 0)
        {
            return false;
        }

        _lastKeywordOcrAt = DateTimeOffset.Now;
        var dialog = Win32Window.FindVisibleDialogByKeywords(source, keywords);
        if (dialog is null)
        {
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
        var unchangedHit = string.Equals(signature, _lastKeywordSignature, StringComparison.OrdinalIgnoreCase);
        if (unchangedHit && _unchangedKeywordNotifyCount >= MaxUnchangedKeywordNotifications)
        {
            _keywordLastHitText.Text = $"\u6765\u6e90\uff1a\u7a97\u53e3\u5f39\u7a97{Environment.NewLine}\u547d\u4e2d\uff1a{hit}{Environment.NewLine}\u672a\u53d1\u9001\uff1a\u5f39\u7a97\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {MaxUnchangedKeywordNotifications} \u6b21\u63d0\u9192\u4e0a\u9650{Environment.NewLine}{dialogText}";
            SetStatus($"\u547d\u4e2d\u5f39\u7a97\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u5f39\u7a97\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {MaxUnchangedKeywordNotifications} \u6b21\u63d0\u9192\u4e0a\u9650");
            return true;
        }

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, (int)_cooldownInput.Value));
        if (normalizedHit == _lastKeywordHit && cooldown > TimeSpan.Zero && DateTimeOffset.Now - _lastKeywordNotifyAt < cooldown)
        {
            SetStatus($"\u547d\u4e2d\u5f39\u7a97\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u51b7\u5374\u4e2d");
            return true;
        }

        _lastKeywordHit = normalizedHit;
        _lastKeywordSignature = signature;
        _lastKeywordNotifyAt = DateTimeOffset.Now;
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
                new NotificationEvent(dialog.Title, 0, Rectangle.Empty, dialogText));
            if (!notified)
            {
                SetStatus("\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u5931\u8d25\uff1a\u6240\u6709\u901a\u77e5\u6e20\u9053\u5747\u53d1\u9001\u5931\u8d25");
                LogWarn($"\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u672a\u6210\u529f\uff1a{hit}\uff1b\u672a\u6d88\u8017\u672a\u53d8\u5316\u63d0\u9192\u6b21\u6570");
                return true;
            }

            _unchangedKeywordNotifyCount = unchangedHit ? _unchangedKeywordNotifyCount + 1 : 1;
            LogInfo($"\u5f39\u7a97\u5173\u952e\u8bcd\u5df2\u63d0\u9192\uff1a{hit}\uff1b\u5f53\u524d\u672a\u53d8\u5316\u8fde\u7eed\u63d0\u9192 {_unchangedKeywordNotifyCount}/{MaxUnchangedKeywordNotifications}");
            return true;
        }
        catch (Exception ex)
        {
            LogError("\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u53d1\u9001\u5931\u8d25", ex);
            SetStatus($"\u5f39\u7a97\u5173\u952e\u8bcd\u901a\u77e5\u5931\u8d25\uff1a{ex.Message}");
            return true;
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

    private bool ShouldRunKeywordCheck()
    {
        if (GetWatchKeywords().Count == 0)
        {
            return false;
        }

        if (IsOcrKeywordDetectionMode() && !_ocrEnabled.Checked)
        {
            return false;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(500, (int)_keywordOcrIntervalInput.Value));
        return DateTimeOffset.Now - _lastKeywordOcrAt >= interval;
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

        var hit = FindKeywordHit(ocrResult.FullText, GetWatchKeywords());
        if (hit is null)
        {
            ResetKeywordNotificationState();
            _keywordLastHitText.Text = string.IsNullOrWhiteSpace(context)
                ? $"\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u672a\u547d\u4e2d\uff1a\u672c\u6b21 OCR \u672a\u8bc6\u522b\u5230\u6587\u672c"
                : $"\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u672a\u547d\u4e2d{Environment.NewLine}{context}";
            SetStatus($"\u5173\u952e\u8bcd\u76d1\u63a7\u4e2d\uff0c\u672a\u547d\u4e2d\uff1a{GetSelectedSourceTitle()}\uff1b{DescribeRegion(region, capture.Size)}");
            return false;
        }

        var normalizedHit = OcrTextParser.NormalizeText(hit);
        var signature = BuildKeywordSignature(normalizedHit, ocrResult.FullText);
        var unchangedHit = string.Equals(signature, _lastKeywordSignature, StringComparison.OrdinalIgnoreCase);
        if (unchangedHit && _unchangedKeywordNotifyCount >= MaxUnchangedKeywordNotifications)
        {
            _keywordLastHitText.Text = $"\u6765\u6e90\uff1a{GetSelectedSourceTitle()}{Environment.NewLine}\u533a\u57df\uff1a{DescribeRegion(region, capture.Size)}{Environment.NewLine}\u547d\u4e2d\uff1a{hit}{Environment.NewLine}\u672a\u53d1\u9001\uff1a\u753b\u9762\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {MaxUnchangedKeywordNotifications} \u6b21\u63d0\u9192\u4e0a\u9650{Environment.NewLine}{context}";
            SetStatus($"\u547d\u4e2d\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u753b\u9762\u672a\u53d8\u5316\uff0c\u5df2\u8fbe\u5230 {MaxUnchangedKeywordNotifications} \u6b21\u63d0\u9192\u4e0a\u9650");
            return false;
        }

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, (int)_cooldownInput.Value));
        if (normalizedHit == _lastKeywordHit && cooldown > TimeSpan.Zero && DateTimeOffset.Now - _lastKeywordNotifyAt < cooldown)
        {
            SetStatus($"\u547d\u4e2d\u5173\u952e\u8bcd\u201c{hit}\u201d\uff0c\u51b7\u5374\u4e2d");
            return false;
        }

        _lastKeywordHit = normalizedHit;
        _lastKeywordSignature = signature;
        _lastKeywordNotifyAt = DateTimeOffset.Now;

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
                new NotificationEvent(_selectedWindow?.Title ?? "", 0, region, ocrResult.FullText));
            if (!notified)
            {
                SetStatus("\u5173\u952e\u8bcd\u901a\u77e5\u5931\u8d25\uff1a\u6240\u6709\u901a\u77e5\u6e20\u9053\u5747\u53d1\u9001\u5931\u8d25");
                LogWarn($"\u5173\u952e\u8bcd\u901a\u77e5\u672a\u6210\u529f\uff1a{hit}\uff1b\u672a\u6d88\u8017\u672a\u53d8\u5316\u63d0\u9192\u6b21\u6570");
                return false;
            }

            _unchangedKeywordNotifyCount = unchangedHit ? _unchangedKeywordNotifyCount + 1 : 1;
            LogInfo($"\u5173\u952e\u8bcd\u5df2\u63d0\u9192\uff1a{hit}\uff1b\u5f53\u524d\u672a\u53d8\u5316\u8fde\u7eed\u63d0\u9192 {_unchangedKeywordNotifyCount}/{MaxUnchangedKeywordNotifications}");
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
        _lastKeywordHit = "";
        _lastKeywordSignature = "";
        _unchangedKeywordNotifyCount = 0;
        _lastKeywordNotifyAt = DateTimeOffset.MinValue;
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

    private IReadOnlyList<string> GetWatchKeywords()
    {
        return _watchKeywords.Text
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

    private void LoadNotificationChannelsFromConfig()
    {
        _notificationChannels.Clear();
        _notificationChannels.AddRange(_config.GetEffectiveNotificationChannels().Select(channel => channel.Clone()));
        if (_notificationChannels.Count == 0)
        {
            _notificationChannels.Add(CreateDefaultNotificationChannel());
        }

        RefreshWebhookChannelList(0);
    }

    private static NotificationChannelConfig CreateDefaultNotificationChannel()
    {
        return new NotificationChannelConfig
        {
            Name = "Webhook",
            Enabled = false,
            Preset = "generic",
            Method = "POST",
            Headers = "Content-Type: application/json",
            BodyTemplate = "{\"title\":\"{title}\",\"body\":\"{body}\",\"createdAt\":\"{time}\",\"window\":\"{window}\",\"distance\":{distance},\"region\":\"{region}\"}"
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
        var name = string.IsNullOrWhiteSpace(channel.Name) ? $"Webhook {index + 1}" : channel.Name.Trim();
        var preset = string.IsNullOrWhiteSpace(channel.Preset) ? "custom" : channel.Preset;
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
            _webhookPreset.SelectedItem = string.IsNullOrWhiteSpace(channel.Preset) ? "custom" : channel.Preset;
            if (_webhookPreset.SelectedItem is null)
            {
                _webhookPreset.SelectedItem = "custom";
            }

            _webhookMethod.Text = string.IsNullOrWhiteSpace(channel.Method) ? "POST" : channel.Method;
            _webhookUrl.Text = channel.Url;
            _webhookHeaders.Text = channel.Headers;
            _webhookBodyTemplate.Text = channel.BodyTemplate;
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
        }
        finally
        {
            _updatingWebhookChannels = false;
        }
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
        channel.Method = string.IsNullOrWhiteSpace(_webhookMethod.Text) ? "POST" : _webhookMethod.Text.Trim();
        channel.Url = _webhookUrl.Text.Trim();
        channel.Headers = _webhookHeaders.Text.Trim();
        channel.BodyTemplate = _webhookBodyTemplate.Text.Trim();
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

        SaveSelectedWebhookChannelFromUi();
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

        _config.PollIntervalMs = (int)_intervalInput.Value;
        _config.CooldownSeconds = (int)_cooldownInput.Value;
        _config.MonitorMode = "keyword";
        _config.KeywordDetectionMode = GetKeywordDetectionModeValue();

        _config.OcrEnabled = _ocrEnabled.Checked;
        _config.OcrMode = _ocrMode.SelectedItem?.ToString() ?? "local";
        _config.OcrUrl = _ocrUrl.Text.Trim();
        _config.OcrCommand = _ocrCommand.Text.Trim();
        _config.OcrArguments = _ocrArguments.Text.Trim();
        _config.MinConfidence = (double)_minConfidenceInput.Value / 100d;
        _config.MaxOcrPixels = (int)_maxOcrPixelsInput.Value * 10_000;
        _config.FullWindowKeywordEnabled = true;
        _config.KeywordOcrIntervalMs = (int)_keywordOcrIntervalInput.Value;
        _config.WatchKeywords = _watchKeywords.Text.Trim();

        SyncWebhookEditorToSelectedChannel();
        _config.NotificationChannels = _notificationChannels.Select(channel => channel.Clone()).ToList();
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
        var keywordOcr = IsOcrKeywordDetectionMode();
        var commandMode = string.Equals(_ocrMode.SelectedItem?.ToString(), "command", StringComparison.OrdinalIgnoreCase);
        var wxMode = string.Equals(_ocrMode.SelectedItem?.ToString(), "wxocr", StringComparison.OrdinalIgnoreCase);
        _ocrUrl.Enabled = keywordOcr && wxMode;
        _minConfidenceInput.Enabled = keywordOcr && wxMode;
        _ocrCommand.Enabled = keywordOcr && commandMode;
        _ocrArguments.Enabled = keywordOcr && commandMode;
        _maxOcrPixelsInput.Enabled = keywordOcr;
    }

    private void UpdateDetectionModeVisibility()
    {
        var ocrMode = IsOcrKeywordDetectionMode();
        foreach (var control in _sourceSelectionControls.Concat(_regionSelectionControls).Concat(_ocrSourceControls))
        {
            control.Visible = ocrMode;
        }

        if (!ocrMode)
        {
            _previewing = false;
            _preview.Image = null;
        }

        _navOcrButton.Enabled = ocrMode;
        _ocrEnabled.Enabled = ocrMode;
        _ocrMode.Enabled = ocrMode;
        _ocrText.PlaceholderText = ocrMode
            ? "\u6700\u8fd1\u4e00\u6b21 OCR \u6587\u672c\u4f1a\u663e\u793a\u5728\u8fd9\u91cc"
            : "外部窗口/弹窗文本命中后会显示在这里";
        UpdateOcrModeVisibility();
    }

    private bool IsOcrKeywordDetectionMode()
    {
        return !IsWindowTextKeywordDetectionMode();
    }

    private bool IsWindowTextKeywordDetectionMode()
    {
        return string.Equals(GetKeywordDetectionModeValue(), "windowText", StringComparison.OrdinalIgnoreCase);
    }

    private string GetKeywordDetectionModeValue()
    {
        return _keywordDetectionMode.SelectedIndex == 1 ? "windowText" : "ocr";
    }

    private static string ResolveKeywordDetectionModeLabel(string mode)
    {
        return string.Equals(mode, "windowText", StringComparison.OrdinalIgnoreCase)
            ? "外部窗口/弹窗文本"
            : "OCR 图像识别";
    }

    private void ResetOcrResultViews()
    {
        if (_loadingConfig)
        {
            return;
        }

        _ocrText.Clear();
        _keywordLastHitText.Clear();
        _ocrText.PlaceholderText = IsOcrKeywordDetectionMode()
            ? "\u6700\u8fd1\u4e00\u6b21 OCR \u6587\u672c\u4f1a\u663e\u793a\u5728\u8fd9\u91cc"
            : "外部窗口/弹窗文本命中后会显示在这里";
        _keywordLastHitText.PlaceholderText = "\u5173\u952e\u8bcd\u547d\u4e2d\u548c\u76d1\u63a7\u533a\u57df\u4f1a\u663e\u793a\u5728\u8fd9\u91cc";
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
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(0, 0, 0, 10);
        if (control is TextBox or ComboBox or NumericUpDown)
        {
            control.MinimumSize = new Size(240, control.MinimumSize.Height);
        }

        panel.Controls.Add(control, 0, panel.RowCount);
        panel.SetColumnSpan(control, 2);
        panel.RowCount++;
    }

    private static Label AddLabeled(TableLayoutPanel panel, string labelText, Control control)
    {
        if (control is ComboBox comboBox &&
            comboBox.Items.Cast<object>().Any(item => string.Equals(item.ToString(), "OCR \u56fe\u50cf\u8bc6\u522b", StringComparison.Ordinal)))
        {
            labelText = "\u8bc6\u522b\u65b9\u5f0f";
        }

        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = MutedText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 3, 8, 10)
        };
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(0, 0, 0, 10);
        control.MinimumSize = new Size(240, control.MinimumSize.Height);
        panel.Controls.Add(label, 0, panel.RowCount);
        panel.Controls.Add(control, 1, panel.RowCount);
        panel.RowCount++;
        return label;
    }

    private static Label AddNumber(TableLayoutPanel panel, string labelText, NumericUpDown input, int min, int max)
    {
        input.Minimum = min;
        input.Maximum = max;
        if (min == 500 && max == 30000)
        {
            labelText = "\u8bc6\u522b\u95f4\u9694\u6beb\u79d2";
        }

        return AddLabeled(panel, labelText, input);
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.Height = 36;
        button.Width = Math.Max(button.Width, 128);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 8, 8);
    }

    private void AddNavButton(FlowLayoutPanel nav, Button button, string text, Action onClick)
    {
        button.Text = text;
        button.Width = 70;
        button.Height = 44;
        button.Margin = new Padding(0, 0, 0, 10);
        button.ForeColor = Color.White;
        button.BackColor = Color.FromArgb(47, 60, 76);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;
        button.Click += (_, _) => onClick();
        nav.Controls.Add(button);
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
            buttons[i].BackColor = i == index ? Accent : Color.FromArgb(47, 60, 76);
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
        SaveConfigFromUi();
        _wxOcrService.Dispose();
        _localOcrService.Dispose();
        _updateService.Dispose();
        _notificationService.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            _preview.Image = null;
        }
    }
}
