namespace MetroREX.Forms;

/// <summary>Campuri de text cu acelasi aspect pe toate ecranele (restul controalelor sunt in <see cref="Ui"/>).</summary>
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

    /// <summary>Lista derulanta in care se poate doar alege (fara text liber).</summary>
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
