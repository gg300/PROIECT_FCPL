namespace MetroREX.Forms;

internal static class Inputs
{
    private static readonly Font InputFont = new(Theme.FontName, 10.5f);

    public static TextBox Text(string placeholder = "") => new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Theme.Paper,
        ForeColor = Theme.Ink,
        Font = InputFont,
        PlaceholderText = placeholder,
    };

    public static ComboBox Choice() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        BackColor = Theme.Paper,
        ForeColor = Theme.Ink,
        Font = InputFont,
    };

    public static TextBox Password(string placeholder = "")
    {
        var box = Text(placeholder);
        box.UseSystemPasswordChar = true;
        return box;
    }
}
