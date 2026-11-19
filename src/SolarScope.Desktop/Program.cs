namespace SolarScope;
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var controller = new AppController();
        Application.Run(controller);
    }
}
