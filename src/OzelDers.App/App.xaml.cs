namespace OzelDers.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

        AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
        {
            try
            {
                var logFile = @"D:\OZELDERS\MauiErrorLogs.txt";
                var errorText = $"[{DateTime.Now:HH:mm:ss}] {e.Exception.GetType().Name}: {e.Exception.Message}\n{e.Exception.StackTrace}\n---\n";
                if (e.Exception.InnerException != null)
                {
                    errorText += $"Inner: {e.Exception.InnerException.Message}\n{e.Exception.InnerException.StackTrace}\n---\n";
                }
                
                // Exclude some spammy MAUI benign exceptions if needed, but logging all is fine.
                System.IO.File.AppendAllText(logFile, errorText);
            }
            catch {}
        };
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "OzelDers.App" };
	}
}
