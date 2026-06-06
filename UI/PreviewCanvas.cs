namespace MhxyNotify.UI;

public sealed class PreviewCanvas : Control
{
    private Bitmap? _image;
    private bool _dragging;
    private Point _dragStartImage;

    public event EventHandler? SelectionChanged;

    public Rectangle SelectedRegion { get; private set; } = Rectangle.Empty;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public Bitmap? Image
    {
        get => _image;
        set
        {
            _image?.Dispose();
            _image = value;
            if (_image is not null && !SelectedRegion.IsEmpty)
            {
                SelectedRegion = Rectangle.Intersect(SelectedRegion, new Rectangle(Point.Empty, _image.Size));
            }

            Invalidate();
        }
    }

    public PreviewCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(24, 28, 34);
        Cursor = Cursors.Cross;
        ResizeRedraw = true;
    }

    public void SetRegion(Rectangle region)
    {
        SelectedRegion = Normalize(region);
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(BackColor);

        if (_image is null)
        {
            TextRenderer.DrawText(
                e.Graphics,
                "\u9009\u62e9\u7a97\u53e3\u540e\u70b9\u51fb\u201c\u5171\u4eab\u9884\u89c8\u201d",
                Font,
                ClientRectangle,
                Color.Gainsboro,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var imageRect = GetImageDisplayRectangle();
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImage(_image, imageRect);

        if (!SelectedRegion.IsEmpty)
        {
            var rect = ImageToDisplay(SelectedRegion);
            using var brush = new SolidBrush(Color.FromArgb(35, Color.Lime));
            using var pen = new Pen(Color.Lime, 2);
            e.Graphics.FillRectangle(brush, rect);
            e.Graphics.DrawRectangle(pen, rect);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_image is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = true;
        _dragStartImage = DisplayToImage(e.Location);
        SelectedRegion = Rectangle.Empty;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging || _image is null)
        {
            return;
        }

        var current = DisplayToImage(e.Location);
        SelectedRegion = ClampToImage(Normalize(Rectangle.FromLTRB(_dragStartImage.X, _dragStartImage.Y, current.X, current.Y)));
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Capture = false;
        SelectedRegion = ClampToImage(SelectedRegion);
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image?.Dispose();
        }

        base.Dispose(disposing);
    }

    private Rectangle GetImageDisplayRectangle()
    {
        if (_image is null)
        {
            return Rectangle.Empty;
        }

        var scale = Math.Min((double)ClientSize.Width / _image.Width, (double)ClientSize.Height / _image.Height);
        if (double.IsInfinity(scale) || scale <= 0)
        {
            return Rectangle.Empty;
        }

        var width = (int)Math.Round(_image.Width * scale);
        var height = (int)Math.Round(_image.Height * scale);
        return new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
    }

    private Point DisplayToImage(Point point)
    {
        if (_image is null)
        {
            return Point.Empty;
        }

        var rect = GetImageDisplayRectangle();
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return Point.Empty;
        }

        var x = (int)Math.Round((point.X - rect.X) * (double)_image.Width / rect.Width);
        var y = (int)Math.Round((point.Y - rect.Y) * (double)_image.Height / rect.Height);
        return new Point(Math.Clamp(x, 0, _image.Width), Math.Clamp(y, 0, _image.Height));
    }

    private Rectangle ImageToDisplay(Rectangle region)
    {
        if (_image is null)
        {
            return Rectangle.Empty;
        }

        var rect = GetImageDisplayRectangle();
        var x = rect.X + (int)Math.Round(region.X * (double)rect.Width / _image.Width);
        var y = rect.Y + (int)Math.Round(region.Y * (double)rect.Height / _image.Height);
        var width = (int)Math.Round(region.Width * (double)rect.Width / _image.Width);
        var height = (int)Math.Round(region.Height * (double)rect.Height / _image.Height);
        return new Rectangle(x, y, width, height);
    }

    private Rectangle ClampToImage(Rectangle region)
    {
        if (_image is null)
        {
            return Rectangle.Empty;
        }

        return Rectangle.Intersect(region, new Rectangle(Point.Empty, _image.Size));
    }

    private static Rectangle Normalize(Rectangle rectangle)
    {
        var x1 = Math.Min(rectangle.Left, rectangle.Right);
        var y1 = Math.Min(rectangle.Top, rectangle.Bottom);
        var x2 = Math.Max(rectangle.Left, rectangle.Right);
        var y2 = Math.Max(rectangle.Top, rectangle.Bottom);
        return Rectangle.FromLTRB(x1, y1, x2, y2);
    }
}
