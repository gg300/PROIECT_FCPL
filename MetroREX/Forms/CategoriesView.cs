using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MetroREX.Controls;

namespace MetroREX.Forms;

internal sealed class CategoriesView : ViewBase
{
    private static readonly Font SearchFont = new(Theme.FontName, 12f);

    private enum SortMode { Name, Products }

    private sealed record Entry(string Name, int Count, CategoryTile Tile);

    private readonly List<Entry> _entries = new();
    private readonly CatalogPage _page;
    private readonly TextBox _search;
    private readonly Button _sortButton;
    private readonly Label _summary;
    private readonly Label _empty;

    private List<Entry> _shown = new();
    private SortMode _sort = SortMode.Name;

    public CategoriesView(ShopForm shell) : base(shell)
    {
        var categories = Services.Catalog.GetCategories();
        var counts = categories.Select(category => Services.Catalog.GetProducts(category).Count).ToList();

        var hero = new Hero(categories.Count, counts.Sum());

        _summary = new Label
        {
            AutoSize = true,
            Font = Ui.BoldFont,
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Left,
            Margin = Padding.Empty,
        };

        _search = Inputs.Text("Cauta o categorie...");
        _search.Font = SearchFont;
        _search.Width = LogicalToDeviceUnits(280);
        _search.Anchor = AnchorStyles.Left;
        _search.Margin = new Padding(0, 0, LogicalToDeviceUnits(12), 0);
        _search.TextChanged += (_, _) => ApplyFilter();
        _search.KeyDown += OnSearchKeyDown;

        _sortButton = Ui.SecondaryButton(this, SortText, (_, _) => ToggleSort());
        _sortButton.Anchor = AnchorStyles.Right;
        _sortButton.Margin = Padding.Empty;

        _empty = Ui.Muted(this, string.Empty);
        _empty.BackColor = Color.Transparent;
        _empty.Visible = false;

        var toolbar = BuildToolbar();
        toolbar.Visible = categories.Count > 0;

        _page = new CatalogPage(hero, toolbar, _empty) { Dock = DockStyle.Fill };

        for (var i = 0; i < categories.Count; i++)
        {
            var category = categories[i];
            var tile = new CategoryTile(category, Ui.ProductCount(counts[i]), i + 1);
            tile.Click += (_, _) => Open(category);

            _entries.Add(new Entry(category, counts[i], tile));
            _page.AddTile(tile);
        }

        Controls.Add(_page);
        ApplyFilter();
    }

    private string SortText => _sort == SortMode.Name ? "Sorteaza: A–Z" : "Sorteaza: cele mai multe produse";

    private TableLayoutPanel BuildToolbar()
    {
        var bar = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        bar.Controls.Add(_summary, 0, 0);
        bar.Controls.Add(_search, 1, 0);
        bar.Controls.Add(_sortButton, 2, 0);
        return bar;
    }

    private void ApplyFilter()
    {
        var query = _search.Text.Trim();

        IEnumerable<Entry> items = _entries;
        if (query.Length > 0)
        {
            items = items.Where(entry => entry.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        items = _sort == SortMode.Products
            ? items.OrderByDescending(entry => entry.Count).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
            : items.OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase);

        _shown = items.ToList();

        _summary.Text = query.Length == 0
            ? CategoryCount(_entries.Count)
            : $"{_shown.Count} din {CategoryCount(_entries.Count)}";

        var emptyText = _entries.Count == 0
            ? "Momentan nu exista produse in magazin."
            : $"Nicio categorie nu se potriveste cu „{query}”.";

        _page.Arrange(_entries.Select(entry => entry.Tile).ToList(), _shown.Select(entry => entry.Tile).ToList(), emptyText);
    }

    private void ToggleSort()
    {
        _sort = _sort == SortMode.Name ? SortMode.Products : SortMode.Name;
        _sortButton.Text = SortText;
        ApplyFilter();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && _search.TextLength > 0)
        {
            _search.Clear();
            e.Handled = e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter && _shown.Count == 1)
        {
            e.Handled = e.SuppressKeyPress = true;
            Open(_shown[0].Name);
        }
    }

    private void Open(string category) => Shell.Navigate(() => new ProductListView(Shell, category));

    private static string CategoryCount(int count) => count == 1
        ? "1 categorie"
        : count % 100 >= 20 || (count > 0 && count % 100 == 0) ? $"{count} de categorii" : $"{count} categorii";

    private sealed class CatalogPage : Panel
    {
        private const int MaxColumns = 4;

        private readonly Hero _hero;
        private readonly Control _toolbar;
        private readonly Label _empty;
        private readonly List<CategoryTile> _all = new();

        private List<CategoryTile> _shown = new();
        private Bitmap? _dotTile;
        private TextureBrush? _dotBrush;
        private int _dotDpi;

        public CatalogPage(Hero hero, Control toolbar, Label empty)
        {
            _hero = hero;
            _toolbar = toolbar;
            _empty = empty;

            DoubleBuffered = true;
            ResizeRedraw = true;
            AutoScroll = true;
            BackColor = Theme.Concrete;

            Controls.Add(hero);
            Controls.Add(toolbar);
            Controls.Add(empty);
        }

        public void AddTile(CategoryTile tile)
        {
            _all.Add(tile);
            Controls.Add(tile);
        }

        public void Arrange(IReadOnlyList<CategoryTile> all, IReadOnlyList<CategoryTile> shown, string emptyText)
        {
            SuspendLayout();

            var visible = new HashSet<CategoryTile>(shown);
            foreach (var tile in all)
            {
                tile.Visible = visible.Contains(tile);
            }

            for (var i = 0; i < shown.Count; i++)
            {
                shown[i].SetNumber(i + 1);
            }

            _shown = shown.ToList();
            _empty.Text = emptyText;
            _empty.Visible = shown.Count == 0;

            AutoScrollPosition = Point.Empty;
            ResumeLayout(true);
            Invalidate(true);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            int pad = LogicalToDeviceUnits(48);
            int width = ClientSize.Width - 2 * pad;
            if (width <= 0)
            {
                return;
            }

            int y = LogicalToDeviceUnits(20);

            var heroShadow = Surface.ShadowPadding(_hero);
            int heroCardHeight = LogicalToDeviceUnits(192);
            var heroBounds = new Rectangle(
                pad - heroShadow.Left,
                y - heroShadow.Top,
                width + heroShadow.Horizontal,
                heroCardHeight + heroShadow.Vertical);
            y += heroCardHeight + heroShadow.Bottom;

            var toolbarBounds = Rectangle.Empty;
            if (_toolbar.Visible)
            {
                int barHeight = LogicalToDeviceUnits(48);
                toolbarBounds = new Rectangle(pad, y, width, barHeight);
                y += barHeight + LogicalToDeviceUnits(8);
            }

            int gap = LogicalToDeviceUnits(6); // tile-urile au deja umbra incorporata
            int minWidth = LogicalToDeviceUnits(250);
            int columns = Math.Clamp((width + gap) / (minWidth + gap), 1, MaxColumns);
            int tileWidth = (width - gap * (columns - 1)) / columns;
            int tileHeight = Math.Clamp((int)(tileWidth * 0.58), LogicalToDeviceUnits(150), LogicalToDeviceUnits(230));

            int bottom = y;
            if (_shown.Count > 0)
            {
                int rows = (_shown.Count + columns - 1) / columns;
                bottom = y + rows * tileHeight + (rows - 1) * gap;
            }
            else if (_empty.Visible)
            {
                bottom = y + LogicalToDeviceUnits(8) + _empty.GetPreferredSize(Size.Empty).Height;
            }

            // Latimea 0: nu apare bara orizontala; doar inaltimea poate depasi fereastra.
            AutoScrollMinSize = new Size(0, bottom + LogicalToDeviceUnits(32));

            int scrollY = AutoScrollPosition.Y;

            _hero.SetBounds(heroBounds.X, heroBounds.Y + scrollY, heroBounds.Width, heroBounds.Height);

            if (_toolbar.Visible)
            {
                _toolbar.SetBounds(toolbarBounds.X, toolbarBounds.Y + scrollY, toolbarBounds.Width, toolbarBounds.Height);
            }

            if (_empty.Visible)
            {
                _empty.Location = new Point(pad, y + LogicalToDeviceUnits(8) + scrollY);
            }

            for (var i = 0; i < _shown.Count; i++)
            {
                int x = pad + i % columns * (tileWidth + gap);
                int ty = y + i / columns * (tileHeight + gap) + scrollY;
                _shown[i].SetBounds(x, ty, tileWidth, tileHeight);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            var scroll = AutoScrollPosition;
            g.TranslateTransform(scroll.X, scroll.Y); // fundalul sta pe continut si se deruleaza cu el

            var view = new Rectangle(-scroll.X, -scroll.Y, ClientSize.Width, ClientSize.Height);

            PaintGlow(g, new PointF(ClientSize.Width * 0.82f, LogicalToDeviceUnits(140)),
                LogicalToDeviceUnits(640), LogicalToDeviceUnits(400), 64);
            PaintGlow(g, new PointF(ClientSize.Width * 0.05f, LogicalToDeviceUnits(1050)),
                LogicalToDeviceUnits(560), LogicalToDeviceUnits(380), 38);

            g.FillRectangle(GetDotBrush(), view);
        }

        private static void PaintGlow(Graphics g, PointF center, float radiusX, float radiusY, int alpha)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(center.X - radiusX, center.Y - radiusY, radiusX * 2, radiusY * 2);

            using var brush = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(alpha, Theme.Rex),
                SurroundColors = new[] { Color.FromArgb(0, Theme.Rex) },
            };
            g.FillPath(brush, path);
        }

        private TextureBrush GetDotBrush()
        {
            if (_dotBrush is not null && _dotDpi == DeviceDpi)
            {
                return _dotBrush;
            }

            _dotBrush?.Dispose();
            _dotTile?.Dispose();

            int cell = Math.Max(8, LogicalToDeviceUnits(26));
            _dotTile = new Bitmap(cell, cell, PixelFormat.Format32bppPArgb);

            using (var tg = Graphics.FromImage(_dotTile))
            using (var dot = new SolidBrush(Theme.Mix(Theme.Concrete, Theme.Ink, 0.13f)))
            {
                tg.SmoothingMode = SmoothingMode.AntiAlias;
                float r = Math.Max(1.2f, LogicalToDeviceUnits(1) * 1.4f);
                tg.FillEllipse(dot, cell / 2f - r, cell / 2f - r, r * 2, r * 2);
            }

            _dotBrush = new TextureBrush(_dotTile, WrapMode.Tile);
            _dotDpi = DeviceDpi;
            return _dotBrush;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dotBrush?.Dispose();
                _dotTile?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class Hero : Control
    {
        private const int Stations = 5;
        private const TextFormatFlags Flags =
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.Top;

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly string _categories;
        private readonly string _products;

        private Bitmap? _backdrop;
        private Font _eyebrow = null!;
        private Font _headline = null!;
        private Font _lead = null!;
        private Font _statNumber = null!;
        private Font _statLabel = null!;
        private bool _fontsReady;
        private int _fontsDpi;
        private float _progress;

        public Hero(int categoryCount, int productCount)
        {
            _categories = categoryCount.ToString();
            _products = productCount.ToString();

            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;

            _timer.Tick += (_, _) => Tick();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _timer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }

        private void Tick()
        {
            _progress = (float)Position(_clock.Elapsed.TotalSeconds);

            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            var card = Surface.CardBounds(this);
            var track = Track(card);
            int band = LogicalToDeviceUnits(18);
            Invalidate(new Rectangle(card.X, (int)track.Y - band, card.Width, band * 2));
        }

        private static double Position(double seconds)
        {
            const double move = 1.3;
            const double dwell = 0.7;
            const double endHold = 1.0;

            int segments = Stations - 1;
            double t = seconds % (segments * (move + dwell) + endHold);

            for (var i = 0; i < segments; i++)
            {
                if (t < move)
                {
                    return (i + Ease(t / move)) / segments;
                }

                t -= move;
                if (t < dwell)
                {
                    return (i + 1.0) / segments;
                }

                t -= dwell;
            }

            return 1.0;
        }

        private static double Ease(double x) => x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;

        private (float Left, float Right, float Y) Track(Rectangle card)
        {
            float inset = LogicalToDeviceUnits(32);
            return (card.X + inset, card.Right - inset, card.Bottom - LogicalToDeviceUnits(34));
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            _backdrop?.Dispose();
            _backdrop = null;
            base.OnSizeChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            var g = e.Graphics;
            EnsureFonts();

            var card = Surface.CardBounds(this);
            _backdrop ??= BuildBackdrop(card);

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_backdrop, new Rectangle(0, 0, Width, Height));

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            PaintTexts(g, card);
            PaintTrack(g, card);
        }

        private Bitmap BuildBackdrop(Rectangle card)
        {
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int radius = LogicalToDeviceUnits(20);
            Surface.DrawShadow(g, card, radius, 10, 6);

            using var path = Theme.RoundedRect(card, radius);
            using (var fill = new SolidBrush(Theme.Ink))
            {
                g.FillPath(fill, path);
            }

            g.SetClip(path, CombineMode.Intersect);

            using (var glowPath = new GraphicsPath())
            {
                float rx = LogicalToDeviceUnits(320);
                float ry = LogicalToDeviceUnits(200);
                float cx = card.Right - LogicalToDeviceUnits(150);
                float cy = card.Y + LogicalToDeviceUnits(40);
                glowPath.AddEllipse(cx - rx, cy - ry, rx * 2, ry * 2);

                using var glow = new PathGradientBrush(glowPath)
                {
                    CenterColor = Color.FromArgb(70, Theme.Rex),
                    SurroundColors = new[] { Color.FromArgb(0, Theme.Rex) },
                };
                g.FillPath(glow, glowPath);
            }

            float stripeWidth = LogicalToDeviceUnits(30);
            float stripeGap = LogicalToDeviceUnits(46);
            for (var i = 0; i < 4; i++)
            {
                var color = i == 0 ? Theme.Mix(Theme.Ink, Theme.Rex, 0.14f) : Theme.Mix(Theme.Ink, Color.White, 0.05f);
                using var pen = new Pen(color, stripeWidth);
                float x = card.Right - LogicalToDeviceUnits(40) - i * (stripeWidth + stripeGap);
                g.DrawLine(pen, x + card.Height, card.Y - 2, x - card.Height * 0.2f, card.Bottom + 2);
            }

            g.ResetClip();

            using (var border = new Pen(Theme.Mix(Theme.Ink, Color.White, 0.14f), 1f))
            {
                g.DrawPath(border, path);
            }

            return bitmap;
        }

        private void PaintTexts(Graphics g, Rectangle card)
        {
            int pad = LogicalToDeviceUnits(32);
            int x = card.X + pad;
            int y = card.Y + LogicalToDeviceUnits(26);
            var muted = Theme.Mix(Theme.Ink, Color.White, 0.62f);

            Draw(g, "CATALOG", _eyebrow, x, y, Theme.Rex);
            y += Measure(g, "CATALOG", _eyebrow).Height + LogicalToDeviceUnits(6);

            const string word = "linia.";
            int prefix = Measure(g, "Alege-ti " + word, _headline).Width - Measure(g, word, _headline).Width;
            Draw(g, "Alege-ti", _headline, x, y, Color.White);
            Draw(g, word, _headline, x + prefix, y, Theme.Rex);
            y += Measure(g, word, _headline).Height + LogicalToDeviceUnits(2);

            Draw(g, "Alege o categorie ca sa vezi produsele.", _lead, x, y, muted);

            if (card.Width < LogicalToDeviceUnits(760))
            {
                return;
            }

            var stats = new[] { (Value: _categories, Label: "CATEGORII"), (Value: _products, Label: "PRODUSE") };
            int right = card.Right - pad;
            int top = card.Y + LogicalToDeviceUnits(30);

            for (var i = 0; i < stats.Length; i++)
            {
                var number = Measure(g, stats[i].Value, _statNumber);
                var label = Measure(g, stats[i].Label, _statLabel);
                int blockWidth = Math.Max(number.Width, label.Width);
                int left = right - blockWidth;

                Draw(g, stats[i].Value, _statNumber, left + blockWidth - number.Width, top, Color.White);
                Draw(g, stats[i].Label, _statLabel, left + blockWidth - label.Width, top + number.Height, Theme.Rex);

                right = left - LogicalToDeviceUnits(22);
                if (i < stats.Length - 1)
                {
                    using var divider = new Pen(Theme.Mix(Theme.Ink, Color.White, 0.16f), 1f);
                    g.DrawLine(divider, right, top + 4, right, top + number.Height + label.Height);
                    right -= LogicalToDeviceUnits(22);
                }
            }
        }

        private void PaintTrack(Graphics g, Rectangle card)
        {
            var (left, right, y) = Track(card);
            float trainX = left + (right - left) * _progress;
            float thickness = LogicalToDeviceUnits(6);

            using (var rail = new Pen(Theme.Mix(Theme.Ink, Color.White, 0.16f), thickness)
                   { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(rail, left, y, right, y);
            }

            if (trainX > left + 1)
            {
                using var done = new Pen(Theme.Rex, thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(done, left, y, trainX, y);
            }

            float r = LogicalToDeviceUnits(9);
            for (var i = 0; i < Stations; i++)
            {
                float x = left + (right - left) * i / (Stations - 1);
                bool passed = trainX >= x - 1;
                var circle = new RectangleF(x - r, y - r, r * 2, r * 2);

                using var fill = new SolidBrush(passed ? Theme.Rex : Theme.Ink);
                using var ring = new Pen(passed ? Color.White : Theme.Mix(Theme.Ink, Color.White, 0.35f), 3f);
                g.FillEllipse(fill, circle);
                g.DrawEllipse(ring, circle);
            }

            float w = LogicalToDeviceUnits(46);
            float h = LogicalToDeviceUnits(18);
            var body = new RectangleF(trainX - w / 2f, y - h / 2f, w, h);

            using (var path = Theme.RoundedRect(body, h / 2f))
            using (var bodyBrush = new SolidBrush(Color.White))
            using (var outline = new Pen(Theme.Ink, 2f))
            {
                g.FillPath(bodyBrush, path);
                g.DrawPath(outline, path);
            }

            using var window = new SolidBrush(Theme.Ink);
            float wr = Math.Max(2f, h * 0.15f);
            for (var i = -1; i <= 1; i++)
            {
                float cx = trainX + i * w * 0.26f;
                g.FillEllipse(window, cx - wr, y - wr, wr * 2, wr * 2);
            }
        }

        private static Size Measure(Graphics g, string text, Font font) =>
            TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), Flags);

        private static void Draw(Graphics g, string text, Font font, int x, int y, Color color)
        {
            var size = Measure(g, text, font);
            TextRenderer.DrawText(g, text, font, new Rectangle(x, y, size.Width + 4, size.Height), color, Flags);
        }

        private void EnsureFonts()
        {
            if (_fontsReady && _fontsDpi == DeviceDpi)
            {
                return;
            }

            DisposeFonts();

            _eyebrow = new Font(Theme.FontName, LogicalToDeviceUnits(12), FontStyle.Bold, GraphicsUnit.Pixel);
            _headline = new Font(Theme.FontName, LogicalToDeviceUnits(38), FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel);
            _lead = new Font(Theme.FontName, LogicalToDeviceUnits(14), FontStyle.Regular, GraphicsUnit.Pixel);
            _statNumber = new Font(Theme.FontName, LogicalToDeviceUnits(40), FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel);
            _statLabel = new Font(Theme.FontName, LogicalToDeviceUnits(12), FontStyle.Bold, GraphicsUnit.Pixel);

            _fontsDpi = DeviceDpi;
            _fontsReady = true;
        }

        private void DisposeFonts()
        {
            if (!_fontsReady)
            {
                return;
            }

            _eyebrow.Dispose();
            _headline.Dispose();
            _lead.Dispose();
            _statNumber.Dispose();
            _statLabel.Dispose();
            _fontsReady = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _backdrop?.Dispose();
                DisposeFonts();
            }

            base.Dispose(disposing);
        }
    }
}
