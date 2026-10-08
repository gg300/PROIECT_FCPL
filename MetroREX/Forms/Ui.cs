using MetroREX.Models;

namespace MetroREX.Forms;

/// <summary>Mici ajutoare ca toate ecranele sa arate la fel (culorile vin din <see cref="Theme"/>).</summary>
internal static class Ui
{
    public static readonly Color Danger = ColorTranslator.FromHtml("#B3261E");
    public static readonly Font TitleFont = new(Theme.FontName, 20f, FontStyle.Bold);
    public static readonly Font BoldFont = new(Theme.FontName, 10f, FontStyle.Bold);

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
        Margin = new Padding(0, host.LogicalToDeviceUnits(8), 0, host.LogicalToDeviceUnits(4)),
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
        label.ForeColor = isError ? Danger : Theme.Rex;
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
        CreateButton(host, text, onClick, Theme.Rex, Color.White, Theme.Rex);

    public static Button SecondaryButton(Control host, string text, EventHandler onClick) =>
        CreateButton(host, text, onClick, Theme.Paper, Theme.Ink, Theme.Ink);

    private static Button CreateButton(
        Control host, string text, EventHandler onClick, Color back, Color fore, Color border)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            BackColor = back,
            ForeColor = fore,
            Font = Theme.Body,
            Cursor = Cursors.Hand,
            MinimumSize = new Size(0, host.LogicalToDeviceUnits(36)),
            Padding = new Padding(host.LogicalToDeviceUnits(10), 0, host.LogicalToDeviceUnits(10), 0),
            Margin = new Padding(0, 0, host.LogicalToDeviceUnits(10), 0),
        };
        button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.BorderSize = 1;
        button.Click += onClick;
        return button;
    }

    // ===== Tabel =====

    public static DataGridView Grid(Control host, params (string Header, float Weight, bool RightAligned)[] columns)
    {
        var grid = new DataGridView
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
            BorderStyle = BorderStyle.FixedSingle,
            GridColor = Theme.Concrete,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = host.LogicalToDeviceUnits(36),
            Font = Theme.Body,
            Margin = Padding.Empty,
        };

        grid.RowTemplate.Height = host.LogicalToDeviceUnits(34);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Ink;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Ink;
        grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
        grid.DefaultCellStyle.SelectionBackColor = Theme.Rex;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(host.LogicalToDeviceUnits(6), 0, host.LogicalToDeviceUnits(6), 0);

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
            }

            grid.Columns.Add(column);
        }

        return grid;
    }

    // ===== Aranjare =====

    /// <summary>O coloana verticala cu margini; randurile se adauga cu <see cref="AddRow"/>.</summary>
    public static TableLayoutPanel Column(Control host)
    {
        var pad = host.LogicalToDeviceUnits(32);
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
        Margin = new Padding(0, host.LogicalToDeviceUnits(8), 0, 0),
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
            Margin = new Padding(0, host.LogicalToDeviceUnits(6), host.LogicalToDeviceUnits(16), host.LogicalToDeviceUnits(6)),
        }, 0, row);

        input.Margin = new Padding(0, host.LogicalToDeviceUnits(4), 0, host.LogicalToDeviceUnits(4));
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
