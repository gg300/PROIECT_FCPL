using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>Autentificare cu nume de utilizator si parola.</summary>
internal sealed class LoginForm : AuthFormBase
{
    private readonly TextBox _username;
    private readonly TextBox _password;

    public LoginForm(ShopServices services)
        : base(
            services,
            title: "Autentificare",
            subtitle: "Intra in cont ca sa vezi comenzile, wish list-ul si cupoanele.",
            submitText: "Autentificare")
    {
        _username = Ui.AddField(this, Fields, "Utilizator", Inputs.Text());
        _password = Ui.AddField(this, Fields, "Parola", Inputs.Password());
        AddRevealToggle(_password);

        ActiveControl = _username;
    }

    protected override void Submit()
    {
        if (string.IsNullOrWhiteSpace(_username.Text))
        {
            Fail("Introdu numele de utilizator.", _username);
            return;
        }

        if (_password.Text.Length == 0)
        {
            Fail("Introdu parola.", _password);
            return;
        }

        // Mesajul pentru utilizator inexistent si pentru parola gresita este acelasi (decis de AuthService).
        var result = Services.Auth.Login(_username.Text, _password.Text);
        if (!result.IsSuccess)
        {
            Fail(result.Error, _password);
            return;
        }

        Complete();
    }
}
