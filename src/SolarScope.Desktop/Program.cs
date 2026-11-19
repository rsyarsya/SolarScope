namespace SolarScope;
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using (var splash = new StartupSplash())
            splash.ShowDialog();

        using var controller = new AppController();
        Application.Run(controller);
    }
}
