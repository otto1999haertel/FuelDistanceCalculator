using AngleSharp.Html.Parser;

namespace FuelDistanceCalculatorTest.PageTests;

public class IndexPageTest : PageTestBase
{
    [Test]
    public async Task CheckEmptyGasStationResponseDoesNotShowUnexecutedTest()
    {
        var response = await _client.GetAsync("/Index");
        Assert.That(response.IsSuccessStatusCode, Is.True, "Index-Seite konnte nicht geladen werden.");
        var htmlContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(htmlContent);
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);
        var copyrightElement = document.QuerySelector("#noOpenStations");
        Assert.That(copyrightElement, Is.Null);
        var searchBtn = document.QuerySelector("#SearchBtn");
        Assert.That(searchBtn, Is.Not.Null);

    }

    // Test 1: Prüft ob der Cookie-Banner korrekt im HTML vorhanden ist
    [Test]
    [TestCase("/Index")]
    [TestCase("/Contact")]
    public async Task CheckCookieBannerIsThereTest(string site)
    {
        var response = await _client.GetAsync(site);
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"{site} konnte nicht geladen werden.");

        var htmlContent = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);

        // 1. Banner-Element vorhanden
        var cookieBanner = document.QuerySelector("#cookieBanner");
        Assert.That(cookieBanner, Is.Not.Null,
            "#cookieBanner fehlt im HTML.");

        // 2. Initial versteckt
        var style = cookieBanner.GetAttribute("style");
        Assert.That(style, Does.Contain("display:none"),
            "Cookie-Banner sollte initial display:none haben.");

        // 3. Akzeptieren-Button vorhanden
        var acceptBtn = cookieBanner.QuerySelector("button[onclick='acceptCookies()']");
        Assert.That(acceptBtn, Is.Not.Null,
            "Akzeptieren-Button fehlt.");

        // 4. Ablehnen-Button vorhanden
        var declineBtn = cookieBanner.QuerySelector("button[onclick='declineCookies()']");
        Assert.That(declineBtn, Is.Not.Null,
            "Ablehnen-Button fehlt.");

        // 5. Datenschutz-Link vorhanden
        var privacyLink = cookieBanner.QuerySelector("a[href='/Contact#datenschutz']");
        Assert.That(privacyLink, Is.Not.Null,
            "Datenschutz-Link fehlt im Banner.");
    }

    // Test 2: Prüft DSGVO-Konformität - GA darf nicht ohne Consent laden
    [Test]
    [TestCase("/Index")]
    [TestCase("/Contact")]
    public async Task GoogleAnalyticsConsentComplianceTest(string site)
    {
        var response = await _client.GetAsync(site);
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"{site} konnte nicht geladen werden.");

        var htmlContent = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);

        // 1. Kein statisches GA-Script-Tag im HTML
        var gaScriptTags = document.QuerySelectorAll("script[src*='googletagmanager.com/gtag/js']");
        Assert.That(gaScriptTags.Length, Is.EqualTo(0),
            "GA darf nicht als statisches <script src=...> eingebunden sein.");

        // 2. Consent default muss auf 'denied' stehen
        Assert.That(htmlContent, Does.Contain("analytics_storage': 'denied'"),
            "GA Consent muss standardmäßig 'denied' sein.");

        // 3. loadGoogleAnalytics() Funktion muss vorhanden sein (für späteres Laden nach Consent)
        Assert.That(htmlContent, Does.Contain("function loadGoogleAnalytics()"),
            "loadGoogleAnalytics() Funktion fehlt.");

        // 4. acceptCookies() Funktion muss vorhanden sein
        Assert.That(htmlContent, Does.Contain("function acceptCookies()"),
            "acceptCookies() Funktion fehlt.");

        // 5. declineCookies() Funktion muss vorhanden sein
        Assert.That(htmlContent, Does.Contain("function declineCookies()"),
            "declineCookies() Funktion fehlt.");
    }
}