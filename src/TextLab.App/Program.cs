namespace Liss.TextLab.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(Theme.Font());
        Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message, "Text Lab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        Application.Run(new MainForm(args));
    }
}
