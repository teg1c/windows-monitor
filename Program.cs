using MhxyNotify.UI;

namespace MhxyNotify;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, AppInfo.MutexName, out var createdNew);
        if (!createdNew)
        {
            AntdUI.Modal.open(AppInfo.FullTitle, "\u7a0b\u5e8f\u5df2\u7ecf\u5728\u8fd0\u884c\u3002", AntdUI.TType.Info);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        AntdUI.Style.Set(AntdUI.Colour.Primary, Color.FromArgb(18, 142, 98));
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBg, Color.FromArgb(230, 247, 239));
        AntdUI.Style.Set(AntdUI.Colour.PrimaryHover, Color.FromArgb(40, 164, 118));
        AntdUI.Style.Set(AntdUI.Colour.PrimaryActive, Color.FromArgb(13, 117, 79));
        AntdUI.Style.Set(AntdUI.Colour.BorderColor, Color.FromArgb(220, 228, 225));
        AntdUI.Style.Set(AntdUI.Colour.BgLayout, Color.FromArgb(244, 247, 246));
        Application.Run(new MainForm());
    }
}
