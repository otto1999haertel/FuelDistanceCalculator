namespace FuelDistanceCalculatorE2ETest;

public abstract class E2EBaseTest : PageTest
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("BASE_URL") ?? "https://localhost";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true, // wegen self-signed Zertifikaten in der E2E-Umgebung
            BaseURL = BaseUrl
        };
    }

    [SetUp]
    public async Task SetUp()
    {
        await Page.GotoAsync("/");

        var cookieButton = Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Ablehnen"
        });

        if (await cookieButton.IsVisibleAsync())
        {
            await cookieButton.ClickAsync();
        }
    }
}