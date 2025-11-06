using AngleSharp.Html.Parser;

namespace FuelDistanceCalculatorTest.PageTests
{
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
    }
}