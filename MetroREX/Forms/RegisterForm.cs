using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>Creare cont: nume de utilizator, parola si profil. La succes utilizatorul este deja autentificat.</summary>
internal sealed class RegisterForm : AuthFormBase
{
    private readonly TextBox _username;
    private readonly TextBox _password;
    private readonly TextBox _confirmPassword;
    private readonly ProfileFields _profile;

    public RegisterForm(ShopServices services)
        : base(
            services,
            title: "Creeaza cont",
            subtitle: "Primesti istoric de comenzi, comenzi active si wish list.",
            submitText: "Creeaza cont")
    {
        AddSection("Cont");
        _username = Ui.AddField(this, Fields, "Utilizator", Inputs.Text("3–20 de caractere"));
        _password = Ui.AddField(this, Fields, "Parola", Inputs.Password("minim 8, litere si cifre"));
        _confirmPassword = Ui.AddField(this, Fields, "Confirma parola", Inputs.Password());
        AddRevealToggle(_password, _confirmPassword);

        AddSection("Profil");
        _profile = new ProfileFields(this, Fields);

        ActiveControl = _username;
    }

    protected override void Submit()
    {
        // Verificarile de mai jos folosesc aceleasi reguli ca AuthService.Register si servesc doar ca
        // sa evidentiem campul gresit; decizia finala ramane a serviciului.
        var usernameError = Validation.Username(_username.Text);
        if (usernameError is not null)
        {
            Fail(usernameError, _username);
            return;
        }

        var passwordError = Validation.Password(_password.Text);
        if (passwordError is not null)
        {
            Fail(passwordError, _password);
            return;
        }

        if (_password.Text != _confirmPassword.Text)
        {
            Fail("Parolele nu coincid.", _confirmPassword);
            return;
        }

        if (_profile.FindProblem() is { } problem)
        {
            Fail(problem.Message, problem.Field);
            return;
        }

        var result = Services.Auth.Register(_username.Text, _password.Text, _profile.ToProfile());
        if (!result.IsSuccess)
        {
            // Dupa validarea de mai sus, singura regula pe care formularul n-o poate verifica singur
            // este unicitatea numelui de utilizator.
            Fail(result.Error, _username);
            return;
        }

        Services.Session.SignIn(result.Value);
        Complete();
    }
}
