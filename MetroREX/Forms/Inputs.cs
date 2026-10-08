namespace MetroREX.Forms;

/// <summary>Campuri de text cu acelasi aspect pe toate ecranele (restul controalelor sunt in <see cref="Ui"/>).</summary>
internal static class Inputs
{
    public static TextBox Text(string placeholder = "") => new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Theme.Paper,
        ForeColor = Theme.Ink,
        Font = Theme.Body,
        PlaceholderText = placeholder,
    };

    public static TextBox Password(string placeholder = "")
    {
        var box = Text(placeholder);
        box.UseSystemPasswordChar = true;
        return box;
    }
}
