using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>Baza ecranelor magazinului. Un ecran este un UserControl afișat în <see cref="ShopForm"/>.</summary>
internal abstract class ViewBase : UserControl
{
    protected ViewBase(ShopForm shell)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        Dock = DockStyle.Fill;
        BackColor = Theme.Concrete;
        ForeColor = Theme.Ink;
        Font = Theme.Body;
        DoubleBuffered = true;
    }

    protected ShopForm Shell { get; }

    protected ShopServices Services => Shell.Services;
}
