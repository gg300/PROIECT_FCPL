using MetroREX.Models;
using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>O problema gasita la validare: mesajul si campul care trebuie corectat.</summary>
internal readonly record struct FieldProblem(string Message, Control Field);

/// <summary>
/// Cele patru campuri ale profilului (nume, adresa, e-mail, telefon), adaugate intr-un tabel creat cu
/// <see cref="Ui.Fields"/>. Se foloseste la creare cont si, mai tarziu, la setari si la finalizarea comenzii
/// pentru utilizatorii fara cont.
/// </summary>
internal sealed class ProfileFields
{
    private readonly TextBox _name;
    private readonly TextBox _address;
    private readonly TextBox _email;
    private readonly TextBox _phone;
    private readonly (TextBox Box, Func<string?, string?> Rule)[] _rules;

    public ProfileFields(Control host, TableLayoutPanel table)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(table);

        _name = Ui.AddField(host, table, "Nume", Inputs.Text());
        _address = Ui.AddField(host, table, "Adresa", Inputs.Text());
        _email = Ui.AddField(host, table, "E-mail", Inputs.Text());
        _phone = Ui.AddField(host, table, "Telefon", Inputs.Text());

        // Aceleasi reguli, in aceeasi ordine, ca in Validation.Profile: formularul doar stie ce camp sa evidentieze.
        _rules = new (TextBox Box, Func<string?, string?> Rule)[]
        {
            (_name, Validation.Name),
            (_address, Validation.Address),
            (_email, Validation.Email),
            (_phone, Validation.Phone),
        };
    }

    /// <summary>Primul camp invalid, sau <c>null</c> daca profilul este corect.</summary>
    public FieldProblem? FindProblem()
    {
        foreach (var (box, rule) in _rules)
        {
            var message = rule(box.Text);
            if (message is not null)
            {
                return new FieldProblem(message, box);
            }
        }

        return null;
    }

    public UserProfile ToProfile() => new UserProfile
    {
        Name = _name.Text,
        Address = _address.Text,
        Email = _email.Text,
        Phone = _phone.Text,
    }.Normalized();

    public void Load(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _name.Text = profile.Name;
        _address.Text = profile.Address;
        _email.Text = profile.Email;
        _phone.Text = profile.Phone;
    }
}
