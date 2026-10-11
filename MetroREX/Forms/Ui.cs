using MetroREX.Controls;
using MetroREX.Models;

namespace MetroREX.Forms;

/// <summary>Mici ajutoare ca toate ecranele sa arate la fel (culorile vin din <see cref="Theme"/>).</summary>
internal static class Ui
{
    public static readonly Color Danger = ColorTranslator.FromHtml("#C62828");
    public static readonly Font TitleFont = new(Theme.FontName, 22f, FontStyle.Bold);
    public static readonly Font BoldFont = new(Theme.FontName, 10f, FontStyle.Bold);
    private static readonly Font ButtonFont = new(Theme.FontName, 10f, FontStyle.Bold);

    // ===== Texte =====

    public static Label Title(Control host, string text) => new()
    {
        Text = text,
        Font = TitleFont,
        ForeColor = Theme.Ink,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, host.LogicalToDeviceUnits(4)),
    };

    public static Label Section(Control host, string text) => new()
    {
        Text = text,
        Font = BoldFont,
        ForeColor = Theme.Ink,
        AutoSize = true,
        Margin = new Padding(0, host.LogicalToDeviceUnits(10), 0, host.LogicalToDeviceUnits(4)),
    };

    public static Label Muted(Control host, string text) => new()
    {
        Text = text,
        ForeColor = Theme.Muted,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, host.LogicalToDeviceUnits(12)),
    };

    /// <summary>Eticheta in care afisam rezultatul ultimei actiuni (succes / eroare).</summary>
    public static Label Message(Control host) => new()
    {
        AutoSize = true,
        ForeColor = Theme.Muted,
        MaximumSize = new Size(host.LogicalToDeviceUnits(900), 0),
        Margin = new Padding(0, host.LogicalToDeviceUnits(8), 0, host.LogicalToDeviceUnits(8)),
    };

    public static void Say(Label label, string text, bool isError = false)
    {
        label.ForeColor = isError ? Danger : Theme.Success;
        label.Text = text;
    }

    public static void Note(Label label, string text)
    {
        label.ForeColor = Theme.Muted;
        label.Text = text;
    }

    public static string RatingText(Product product) => product.RatingCount == 0
        ? "Fara evaluari"
        : $"{product.Rating:0.0} / 5 (evaluari: {product.RatingCount})";

    public static string ProductCount(int count) => count == 1
        ? "1 produs"
        : count % 100 >= 20 || (count > 0 && count % 100 == 0) ? $"{count} de produse" : $"{count} produse";

    // ===== Butoane =====

    public static Button PrimaryButton(Control host, string text, EventHandler onClick) =>
        CreateButton(host, text, onClick, ButtonKind.Primary);

    public static Button SecondaryButton(Control host, string text, EventHandler onClick) =>
        CreateButton(host, text, onClick, ButtonKind.Secondary);

    /// <summary>Buton mare in stil „card”: titlul pe primul rand, detaliul (muted) pe al doilea.</summary>
    public static Button CardButton(Control host, string title, string detail, EventHandler onClick)
    {
        var button = CreateButton(host, $"{title}{Environment.NewLine}{detail}", onClick, ButtonKind.Card);
        button.AutoSize = false;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, host.LogicalToDeviceUnits(16), host.LogicalToDeviceUnits(16));
        return button;
    }

    /// <summary>Buton transparent pentru antetul negru (text alb, linie portocalie cand e sectiunea curenta).</summary>
    public static RoundedButton NavButton(Control host, string text, EventHandler onClick, bool arrow = false)
    {
        var button = CreateButton(host, text, onClick, ButtonKind.Ghost);
        button.Arrow = arrow;
        button.Margin = new Padding(0, 0, host.LogicalToDeviceUnits(4), 0);
        return button;
    }

    /// <summary>Tile mare, negru, pentru categorii: numarul de produse sus, numele jos, devine portocaliu la hover.</summary>
    public static Button TileButton(Control host, string title, string detail, EventHandler onClick)
    {
        var button = CreateButton(host, $"{title}{Environment.NewLine}{detail}", onClick, ButtonKind.Tile);
        button.AutoSize = false;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, host.LogicalToDeviceUnits(10), host.LogicalToDeviceUnits(10));
        return button;
    }

    private static RoundedButton CreateButton(Control host, string text, EventHandler onClick, ButtonKind kind)
    {
        var button = new RoundedButton
        {
            Kind = kind,
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Font = ButtonFont,
            Cursor = Cursors.Hand,
            MinimumSize = new Size(0, host.LogicalToDeviceUnits(40)),
            Padding = new Padding(host.LogicalToDeviceUnits(10), 0, host.LogicalToDeviceUnits(10), 0),
            Margin = new Padding(0, 0, host.LogicalToDeviceUnits(10), 0),
        };
        button.Click += onClick;
        return button;
    }

    // ===== Tabel =====

    /// <summary>DataGridView cu double buffering, ca derularea sa nu palpaie.</summary>
    private sealed class SmoothGrid : DataGridView
    {
        public SmoothGrid() => DoubleBuffered = true;
    }

    public static DataGridView Grid(Control host, params (string Header, float Weight, bool RightAligned)[] columns)
    {
        var grid = new SmoothGrid
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Theme.Paper,
            BorderStyle = BorderStyle.None,
            GridColor = Theme.Line,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = host.LogicalToDeviceUnits(42),
            Font = Theme.Body,
            Margin = Padding.Empty,
        };

        var cellPad = new Padding(host.LogicalToDeviceUnits(12), 0, host.LogicalToDeviceUnits(12), 0);

        grid.RowTemplate.Height = host.LogicalToDeviceUnits(42);

        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Ink;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Ink;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = cellPad;

        grid.DefaultCellStyle.BackColor = Theme.Paper;
        grid.DefaultCellStyle.ForeColor = Theme.Ink;
        grid.DefaultCellStyle.SelectionBackColor = Theme.RexSoft;
        grid.DefaultCellStyle.SelectionForeColor = Theme.Ink;
        grid.DefaultCellStyle.Padding = cellPad;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.Zebra;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.RexSoft;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Theme.Ink;

        foreach (var (header, weight, rightAligned) in columns)
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            };

            if (rightAligned)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            grid.Columns.Add(column);
        }

        return grid;
    }

    // ===== Aranjare =====

    /// <summary>O coloana verticala cu margini; randurile se adauga cu <see cref="AddRow"/>.</summary>
    public static TableLayoutPanel Column(Control host)
    {
        var pad = host.LogicalToDeviceUnits(48); // aceeasi margine ca antetul
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(pad, pad / 2, pad, pad / 2),
            BackColor = Theme.Concrete,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    /// <summary>Adauga un rand; maximum un rand <paramref name="fill"/> pe tabel (ocupa spatiul ramas).</summary>
    public static void AddRow(TableLayoutPanel table, Control control, bool fill = false)
    {
        table.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
        table.Controls.Add(control, 0, table.RowStyles.Count - 1);
    }

    /// <summary>Un rand orizontal de butoane / controale.</summary>
    public static FlowLayoutPanel Bar(Control host) => new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = false,
        FlowDirection = FlowDirection.LeftToRight,
        BackColor = Color.Transparent,
        Margin = new Padding(0, host.LogicalToDeviceUnits(12), 0, 0),
    };

    /// <summary>Tabel „eticheta: camp”, cu randuri adaugate prin <see cref="AddField{T}"/>.</summary>
    public static TableLayoutPanel Fields(Control host)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    /// <summary>Un rand de titlu de sectiune, pe toata latimea unui tabel creat cu <see cref="Fields"/>.</summary>
    public static void AddSection(Control host, TableLayoutPanel table, string text)
    {
        var label = Section(host, text);
        var row = table.RowStyles.Count;

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(label, 0, row);
        table.SetColumnSpan(label, 2);
    }

    public static T AddField<T>(Control host, TableLayoutPanel table, string label, T input) where T : Control
    {
        var row = table.RowStyles.Count;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        table.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = Theme.Muted,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, host.LogicalToDeviceUnits(8), host.LogicalToDeviceUnits(16), host.LogicalToDeviceUnits(8)),
        }, 0, row);

        input.Margin = new Padding(0, host.LogicalToDeviceUnits(5), 0, host.LogicalToDeviceUnits(5));
        if (input is Label)
        {
            input.Anchor = AnchorStyles.Left;
        }
        else
        {
            input.Dock = DockStyle.Fill;
        }

        table.Controls.Add(input, 1, row);
        return input;
    }
}
