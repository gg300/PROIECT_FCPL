using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>
/// Baza ferestrelor de autentificare (Autentificare / Creare cont): aceeasi infatisare, aceleasi taste
/// (Enter trimite, Esc anuleaza) si aceeasi tratare a erorilor. Clasele derivate adauga doar campurile si
/// regula din <see cref="Submit"/>. Fereastra se inchide cu <see cref="DialogResult.OK"/> dupa ce
/// utilizatorul a intrat in cont (<c>Session.CurrentUser</c> este setat).
/// </summary>
internal abstract class AuthFormBase : Form
{
    private const int ContentWidth = 420;
    private const int MessageLines = 2;

    private readonly Label _message;

    protected AuthFormBase(ShopServices services, string title, string subtitle, string submitText)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(subtitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(submitText);

        Text = $"MetroREX – {title}";
        BackColor = Theme.Concrete;
        ForeColor = Theme.Ink;
        Font = Theme.Body;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink; // inaltimea se potriveste singura cu numarul de campuri
        DoubleBuffered = true;

        var contentWidth = LogicalToDeviceUnits(ContentWidth);
        var inset = LogicalToDeviceUnits(32);

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(inset, inset / 2, inset, inset / 2),
            MinimumSize = new Size(contentWidth + 2 * inset, 0),
            BackColor = Theme.Concrete,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var intro = Ui.Muted(this, subtitle);
        intro.MaximumSize = new Size(contentWidth, 0);

        Fields = Ui.Fields(this);

        // Zona de mesaj are inaltime fixa: fereastra nu "sare" cand apare sau dispare o eroare.
        _message = new Label
        {
            AutoSize = false,
            AutoEllipsis = true,
            Size = new Size(contentWidth, MessageLines * Font.Height),
            ForeColor = Theme.Muted,
            Margin = new Padding(0, LogicalToDeviceUnits(8), 0, LogicalToDeviceUnits(8)),
        };

        var submit = Ui.PrimaryButton(this, submitText, OnSubmitClicked);
        var cancel = Ui.SecondaryButton(this, "Anuleaza", (_, _) => Close());
        var buttons = Ui.Bar(this);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(submit);

        Ui.AddRow(root, Ui.Title(this, title));
        Ui.AddRow(root, intro);
        Ui.AddRow(root, Fields);
        Ui.AddRow(root, _message);
        Ui.AddRow(root, buttons);
        Controls.Add(root);

        AcceptButton = submit;
        CancelButton = cancel;
    }

    protected ShopServices Services { get; }

    /// <summary>Tabelul „eticheta: camp”; clasele derivate adauga aici randurile lor.</summary>
    protected TableLayoutPanel Fields { get; }

    /// <summary>Verifica datele si, daca sunt corecte, incheie autentificarea cu <see cref="Complete"/>.</summary>
    protected abstract void Submit();

    protected void Fail(string error, Control? focus = null)
    {
        Ui.Say(_message, error, isError: true);

        if (focus is null)
        {
            return;
        }

        focus.Focus();
        if (focus is TextBox box)
        {
            box.SelectAll();
        }
    }

    protected void Complete() => DialogResult = DialogResult.OK;

    /// <summary>Un rand de titlu de sectiune, pe toata latimea tabelului de campuri.</summary>
    protected void AddSection(string text)
    {
        var label = Ui.Section(this, text);
        var row = Fields.RowStyles.Count;

        Fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Fields.Controls.Add(label, 0, row);
        Fields.SetColumnSpan(label, 2);
    }

    /// <summary>Adauga „Arata parola”, care dezvaluie sau ascunde toate campurile de parola date.</summary>
    protected void AddRevealToggle(params TextBox[] passwordBoxes)
    {
        var toggle = new CheckBox
        {
            Text = "Arata parola",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Cursor = Cursors.Hand,
        };

        toggle.CheckedChanged += (_, _) =>
        {
            foreach (var box in passwordBoxes)
            {
                box.UseSystemPasswordChar = !toggle.Checked;
            }
        };

        Ui.AddField(this, Fields, string.Empty, toggle);
    }

    private void OnSubmitClicked(object? sender, EventArgs e)
    {
        Ui.Note(_message, string.Empty);

        // Hash-ul parolei (PBKDF2) dureaza cateva zeci de milisecunde, iar serviciile ruleaza pe firul UI.
        UseWaitCursor = true;
        try
        {
            Submit();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Fail($"Datele nu au putut fi salvate: {exception.Message}");
        }
        finally
        {
            UseWaitCursor = false;
        }
    }
}
