using AngleSharp.Html.Parser;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
namespace FuelDistanceCalculatorTest.PageTests
{
    public class ContactPageTest : PageTestBase
    {
       [Test]
        public async Task ContactPage_LibrariesMatchLibmanVersions()
        {
            // 1. Lese libman.json
            var libmanPath = TestContext.CurrentContext.TestDirectory +  "/libman.json"; // Pfad anpassen
            var libmanContent = File.ReadAllText(libmanPath);
            var libmanJson = JObject.Parse(libmanContent);
            var libraries = libmanJson["libraries"]!
                .ToDictionary(
                    lib => lib["library"]!.ToString().Split('@')[0], // Bibliotheksname (ohne Version)
                    lib => lib["library"]!.ToString().Split('@')[1]  // Version
                );

            // 2. Lade die Contact-Seite
            var response = await _client.GetAsync("/Contact");
            Assert.That(response.IsSuccessStatusCode, Is.True, "Contact-Seite konnte nicht geladen werden.");
            var htmlContent = await response.Content.ReadAsStringAsync();

            // 3. Parse das HTML mit AngleSharp
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(htmlContent);
            var libraryList = document.QuerySelectorAll("#externalLibraries li");

            var pageLibraries = new Dictionary<string, string>();
            foreach (var li in libraryList)
            {
                var id = li.Id;
                var text = li.TextContent;
                var version = text.Split(' ')[1]; // Extrahiere Version, z.B. "5.3.7"
                pageLibraries[id] = version;
            }

            // 4. Vergleiche die Versionen
            foreach (var lib in libraries)
            {
                var libName = lib.Key.Replace("jquery-validate", "jquery-validation")
                                  .Replace("jquery-validation-unobtrusive", "jquery-validation-unobtrusive");
                Assert.That(pageLibraries.ContainsKey(libName), $"Bibliothek {libName} nicht auf der Contact-Seite gefunden.");
                Assert.That(pageLibraries[libName], Is.EqualTo(lib.Value), $"Version für {libName} stimmt nicht: erwartet {lib.Value}, gefunden {pageLibraries[libName]}.");
            }
        }
    }
}