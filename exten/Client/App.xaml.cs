namespace Client;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new MainPage();
        MainPage.Disappearing += MainPageOnDisappearing;
    }

    private static void MainPageOnDisappearing(object sender, EventArgs e)
    {
        SecureStorage.Remove("nick");
    }
}