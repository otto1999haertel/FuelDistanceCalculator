using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FuelDistanceCalculatorTest
{
    [TestFixture]
    public class ApiThrottleTests
    {
        [Test]
        public async Task ExecuteWithThrottle_FirstCall_NoDelay()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
            string apiKey = "test-key";
            var expectedResult = "TestResult";
            Func<Task<string>> apiCall = () => Task.FromResult(expectedResult);
            var interval = TimeSpan.FromMilliseconds(100);

            // Act
            var startTime = DateTime.Now;
            var result = await throttle.ExecuteWithThrottle(apiKey, apiCall, interval);
            var elapsedTime = DateTime.Now - startTime;

            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(50), "No significant delay expected for first call"); // Kleiner Puffer für Systemverzögerungen
        }

        [Test]
        public async Task ExecuteWithThrottle_SecondCallWithinInterval_DelaysExecution()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
            string apiKey = "test-key";
            var expectedResult = "TestResult";
            Func<Task<string>> apiCall = () => Task.FromResult(expectedResult);
            var interval = TimeSpan.FromMilliseconds(500);

            // Erster Aufruf
            await throttle.ExecuteWithThrottle(apiKey, apiCall, interval);

            // Act: Zweiter Aufruf kurz danach
            var startTime = DateTime.Now;
            var result = await throttle.ExecuteWithThrottle(apiKey, apiCall, interval);
            var elapsedTime = DateTime.Now - startTime;

            // Assert (Berücksichtigt Jitter ±20%, also ca. 400-600ms, mit kleinem Puffer für Systemzeit)
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(380), $"Expected delay around 500ms (with jitter), but was {elapsedTime.TotalMilliseconds}ms");
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThanOrEqualTo(620), $"Expected delay around 500ms (with jitter), but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public async Task ExecuteWithThrottle_ConcurrentCalls_RespectsSemaphore()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
            string apiKey = "test-key";
            var delay = TimeSpan.FromMilliseconds(200); // Kleineres Delay für schnelleren Test
            Func<Task<string>> apiCall = async () =>
            {
                await Task.Delay(delay); // Simuliert langlaufende API
                return "TestResult";
            };

            // Act: Starte zwei Aufrufe gleichzeitig
            var task1 = throttle.ExecuteWithThrottle(apiKey, apiCall, TimeSpan.FromMilliseconds(100));
            await Task.Delay(10); // Kurze Wartezeit, um zweiten Aufruf zu starten
            var task2 = throttle.ExecuteWithThrottle(apiKey, apiCall, TimeSpan.FromMilliseconds(100));
            var startTime = DateTime.Now;
            await Task.WhenAll(task1, task2);
            var elapsedTime = DateTime.Now - startTime;

            // Assert (Erwarte sequentielle Ausführung: >= 400ms, mit Puffer)
            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(380), $"Expected sequential execution (at least 400ms), but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public async Task ExecuteWithThrottle_DifferentApiKeys_NoMutualDelay()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
            string apiKey1 = "key1";
            string apiKey2 = "key2";
            var expectedResult = "TestResult";
            Func<Task<string>> apiCall = () => Task.FromResult(expectedResult);
            var interval = TimeSpan.FromMilliseconds(500);

            // Act: Aufrufe für unterschiedliche Keys kurz hintereinander
            var task1 = throttle.ExecuteWithThrottle(apiKey1, apiCall, interval);
            await Task.Delay(10); // Kurze Wartezeit
            var task2 = throttle.ExecuteWithThrottle(apiKey2, apiCall, interval);
            var startTime = DateTime.Now;
            await Task.WhenAll(task1, task2);
            var elapsedTime = DateTime.Now - startTime;

            // Assert: Keine gegenseitige Verzögerung, Aufrufe sollten parallel ablaufen (bis auf Semaphore)
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(100), "No delay between different keys expected");
        }

        [Test]
        public async Task ExecuteWithThrottle_NullApiKey_ThrowsArgumentNullException()
        {
            // Arrange
            var throttle = new ApiThrottle();
            Func<Task<string>> apiCall = () => Task.FromResult("TestResult");

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await throttle.ExecuteWithThrottle(null, apiCall));
        }

        [Test]
        public async Task ExecuteWithThrottle_NegativeInterval_UsesPositiveDelay()
        {
            // Arrange
            var throttle = new ApiThrottle();
            string apiKey = "test-key";
            Func<Task<string>> apiCall = () => Task.FromResult("TestResult");
            var negativeInterval = TimeSpan.FromMilliseconds(-100); // Ungültiges Intervall

            // Act
            var startTime = DateTime.Now;
            await throttle.ExecuteWithThrottle(apiKey, apiCall, negativeInterval);
            var elapsedTime = DateTime.Now - startTime;

            // Assert: Kein Delay, da negative Intervalle ignoriert werden sollten (basierend auf Code: baseDelay > 0 prüft)
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(50), "No delay for negative intervals");
        }

        [Test]
        public async Task ExecuteWithThrottle_FailingApiCall_ReleasesSemaphoreAndThrowsException()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
            string apiKey = "test-key";
            Func<Task<string>> failingApiCall = () => throw new InvalidOperationException("API failed");

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await throttle.ExecuteWithThrottle(apiKey, failingApiCall));

            // Prüfe, ob Semaphore freigegeben wurde (indirekt: Ein zweiter Aufruf sollte ohne Blockierung laufen)
            Assert.That(exception.Message, Is.EqualTo("API failed"));
            var successfulCall = await throttle.ExecuteWithThrottle(apiKey, () => Task.FromResult("Success")); // Sollte nicht blockiert sein
            Assert.That(successfulCall, Is.EqualTo("Success"));
        }

        [Test]
        public async Task ExecuteWithThrottle_JitterVariesDelay()
        {
            // Arrange
            var throttle = new ApiThrottle();
            string apiKey = "test-key";
            Func<Task<string>> apiCall = () => Task.FromResult("TestResult");
            var interval = TimeSpan.FromMilliseconds(500);
            var elapsedTimes = new List<double>();

            // Erster Aufruf
            await throttle.ExecuteWithThrottle(apiKey, apiCall, interval);

            // Act: Mehrere zweite Aufrufe, um Jitter zu beobachten
            for (int i = 0; i < 5; i++)
            {
                var startTime = DateTime.Now;
                await throttle.ExecuteWithThrottle(apiKey, apiCall, interval);
                var elapsedTime = DateTime.Now - startTime;
                elapsedTimes.Add(elapsedTime.TotalMilliseconds);
            }

            // Assert: Verzögerungen sollten variieren (im Bereich ca. 400-600ms durch ±20% Jitter)
            var minElapsed = elapsedTimes.Min();
            var maxElapsed = elapsedTimes.Max();
            Assert.That(minElapsed, Is.GreaterThanOrEqualTo(380), "Min delay with jitter");
            Assert.That(maxElapsed, Is.LessThanOrEqualTo(620), "Max delay with jitter");
            Assert.That(maxElapsed - minElapsed, Is.GreaterThan(20), "Jitter should cause variation"); // Überprüft, ob Variation vorhanden ist
        }

        [Test]
        public async Task ExecuteWithThrottle_MaxConcurrentCallsGreaterThanOne_AllowsParallelExecution()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 2);
            string apiKey = "test-key";
            var delay = TimeSpan.FromMilliseconds(200);
            Func<Task<string>> apiCall = async () =>
            {
                await Task.Delay(delay);
                return "TestResult";
            };

            // Act: Starte zwei Aufrufe gleichzeitig
            var task1 = throttle.ExecuteWithThrottle(apiKey, apiCall, TimeSpan.FromMilliseconds(100));
            await Task.Delay(10);
            var task2 = throttle.ExecuteWithThrottle(apiKey, apiCall, TimeSpan.FromMilliseconds(100));
            var startTime = DateTime.Now;
            await Task.WhenAll(task1, task2);
            var elapsedTime = DateTime.Now - startTime;

            // Assert: Parallele Ausführung: ca. 200ms (nicht 400ms)
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(300), $"Expected parallel execution (around 200ms), but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public async Task ExecuteWithThrottle_LoggingOccursCorrectly()
        {
            // Arrange
            var throttle = new ApiThrottle();
            string apiKey = "test-key";
            Func<Task<string>> apiCall = () => Task.FromResult("TestResult");
            var outputBuilder = new StringBuilder();
            var originalOut = Console.Out;
            Console.SetOut(new StringWriter(outputBuilder));

            // Act
            await throttle.ExecuteWithThrottle(apiKey, apiCall, TimeSpan.FromMilliseconds(100));

            // Assert
            Console.SetOut(originalOut); // Zurücksetzen
            var logOutput = outputBuilder.ToString();
            Assert.That(logOutput, Contains.Substring("API-Throttle entered"));
            Assert.That(logOutput, Contains.Substring("Executing API call"));
            Assert.That(logOutput, Contains.Substring("Releasing semaphore"));
        }
    }
}