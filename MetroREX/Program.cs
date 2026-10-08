using MetroREX.Forms;
using MetroREX.Services;
 
namespace MetroREX;
 
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var services = ShopServices.Create();
        Application.Run(new MainForm(services));
    }
}