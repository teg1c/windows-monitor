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
            MessageBox.Show(
                "\u7a0b\u5e8f\u5df2\u7ecf\u5728\u8fd0\u884c\u3002",
                AppInfo.FullTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
