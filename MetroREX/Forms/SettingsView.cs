namespace MetroREX.Forms;

/// <summary>Setari: schimbarea parolei, a profilului (nume, adresa, e-mail, telefon) si stergerea contului.</summary>
internal sealed class SettingsView : ViewBase
{
    private readonly Label _message;
    private readonly ProfileFields _profile;
    private readonly TextBox _currentPassword;
    private readonly TextBox _newPassword;
    private readonly TextBox _confirmPassword;
    private readonly TextBox _deletePassword;

    public SettingsView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Setari"));
        Ui.AddRow(column, Ui.Muted(this, $"Contul: {Services.Session.CurrentUser?.Username}"));

        _message = Ui.Message(this);
        Ui.AddRow(column, _message);

        // Cele trei sectiuni una sub alta; zona se deruleaza pe ferestre mici.
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // ===== Profil =====
        var profileFields = NarrowFields();
        Ui.AddSection(this, profileFields, "Profil");
        _profile = new ProfileFields(this, profileFields);
        if (Services.Session.CurrentUser is { } user)
        {
            _profile.Load(user.Profile);
        }

        Ui.AddRow(stack, profileFields);
        Ui.AddRow(stack, ButtonRow(Ui.PrimaryButton(this, "Salveaza profilul", (_, _) => SaveProfile())));

        // ===== Parola =====
        var passwordFields = NarrowFields();
        Ui.AddSection(this, passwordFields, "Schimba parola");
        _currentPassword = Ui.AddField(this, passwordFields, "Parola curenta", Inputs.Password());
        _newPassword = Ui.AddField(this, passwordFields, "Parola noua", Inputs.Password("minim 8, litere si cifre"));
        _confirmPassword = Ui.AddField(this, passwordFields, "Confirma parola", Inputs.Password());
        Ui.AddRow(stack, passwordFields);
        Ui.AddRow(stack, ButtonRow(Ui.PrimaryButton(this, "Schimba parola", (_, _) => ChangePassword())));

        // ===== Stergere cont =====
        var deleteFields = NarrowFields();
        Ui.AddSection(this, deleteFields, "Sterge contul");
        var warning = Ui.Muted(this, "Se sterg definitiv contul, wish list-ul, cupoanele si istoricul comenzilor.");
        warning.MaximumSize = new Size(LogicalToDeviceUnits(520), 0);
        var row = deleteFields.RowStyles.Count;
        deleteFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        deleteFields.Controls.Add(warning, 0, row);
        deleteFields.SetColumnSpan(warning, 2);
        _deletePassword = Ui.AddField(this, deleteFields, "Parola", Inputs.Password());
        Ui.AddRow(stack, deleteFields);

        var deleteButton = Ui.SecondaryButton(this, "Sterge contul", (_, _) => DeleteAccount());
        deleteButton.ForeColor = Ui.Danger;
        Ui.AddRow(stack, ButtonRow(deleteButton));

        Ui.AddRow(column, stack, fill: true);
        Controls.Add(column);
    }

    /// <summary>Tabel de campuri cu latime limitata, ca formularele sa nu se intinda pe tot ecranul.</summary>
    private TableLayoutPanel NarrowFields()
    {
        var fields = Ui.Fields(this);
        fields.Dock = DockStyle.None;
        fields.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        fields.Width = LogicalToDeviceUnits(560);
        fields.MinimumSize = new Size(LogicalToDeviceUnits(560), 0);
        return fields;
    }

    private FlowLayoutPanel ButtonRow(Control button)
    {
        var bar = Ui.Bar(this);
        bar.Margin = new Padding(0, LogicalToDeviceUnits(4), 0, LogicalToDeviceUnits(20));
        bar.Controls.Add(button);
        return bar;
    }

    // ===== Actiuni =====

    private void SaveProfile()
    {
        if (_profile.FindProblem() is { } problem)
        {
            Fail(problem.Message, problem.Field);
            return;
        }

        var result = Services.Auth.UpdateProfile(_profile.ToProfile());
        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            return;
        }

        Sfx.Play(Sound.Tick);
        Ui.Say(_message, "Profilul a fost salvat.");
    }

    private void ChangePassword()
    {
        if (_newPassword.Text != _confirmPassword.Text)
        {
            Fail("Parolele noi nu coincid.", _confirmPassword);
            return;
        }

        var result = Services.Auth.ChangePassword(_currentPassword.Text, _newPassword.Text);
        if (!result.IsSuccess)
        {
            Fail(result.Error, _currentPassword);
            return;
        }

        _currentPassword.Clear();
        _newPassword.Clear();
        _confirmPassword.Clear();
        Sfx.Play(Sound.Tick);
        Ui.Say(_message, "Parola a fost schimbata.");
    }

    private void DeleteAccount()
    {
        if (_deletePassword.Text.Length == 0)
        {
            Fail("Introdu parola ca sa confirmi stergerea.", _deletePassword);
            return;
        }

        var answer = MessageBox.Show(
            this,
            "Sigur vrei sa stergi contul? Actiunea nu poate fi anulata.",
            "MetroREX – Sterge contul",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        var result = Services.Auth.DeleteAccount(_deletePassword.Text);
        if (!result.IsSuccess)
        {
            Fail(result.Error, _deletePassword);
            return;
        }

        MessageBox.Show(this, "Contul a fost sters.", "MetroREX", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Shell.Close(); // inapoi la meniul principal
    }

    private void Fail(string error, Control field)
    {
        Ui.Say(_message, error, isError: true);
        field.Focus();
        if (field is TextBox box)
        {
            box.SelectAll();
        }
    }
}
