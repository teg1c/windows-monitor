#if NO_ANTDUI
namespace AntdUI;

public class Window : Form
{
    public bool Resizable { get; set; }
    public bool Dark { get; set; }
    public bool AutoHandDpi { get; set; }
}

public class Button : System.Windows.Forms.Button
{
    public object? Type { get; set; }
    public bool Ghost { get; set; }
    public int Radius { get; set; }
}

public class Panel : System.Windows.Forms.Panel
{
    public int Radius { get; set; }
    public int Shadow { get; set; }
}
#endif
