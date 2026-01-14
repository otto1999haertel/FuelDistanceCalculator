using FuelDistanceCalculator.Services;

namespace FuelDistanceCalculatorTest.ServiceTests
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
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(50), $"No significant delay expected for first call, but was {elapsedTime.TotalMilliseconds}ms");
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

            // Assert (Jitter ±20%: 400-600ms, mit Puffer)
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(380), $"Expected delay around 500ms, but was {elapsedTime.TotalMilliseconds}ms");
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThanOrEqualTo(620), $"Expected delay around 500ms, but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public async Task ExecuteWithThrottle_ConcurrentCalls_RespectsSemaphore()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
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

            // Assert: Sequentielle Ausführung (>= 400ms)
            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(380), $"Expected sequential execution (>= 400ms), but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public async Task ExecuteWithThrottle_DifferentApiKeys_AllowsParallelExecutionWithNoThrottlingDelay()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 2); // Erlaubt zwei gleichzeitige Aufrufe
            string apiKey1 = "key1";
            string apiKey2 = "key2";
            var expectedResult = "TestResult";
            Func<Task<string>> apiCall = async () =>
            {
                await Task.Delay(50); // Simuliert eine kurze API-Ausführung
                return expectedResult;
            };
            var interval = TimeSpan.FromMilliseconds(500);

            // Act: Aufrufe für unterschiedliche Keys
            var task1 = throttle.ExecuteWithThrottle(apiKey1, apiCall, interval);
            await Task.Delay(10);
            var task2 = throttle.ExecuteWithThrottle(apiKey2, apiCall, interval);
            var startTime = DateTime.Now;
            await Task.WhenAll(task1, task2);
            var elapsedTime = DateTime.Now - startTime;

            // Assert: Parallele Ausführung (~50ms, kein Drosselungsdelay)
            Assert.That(task1.Result, Is.EqualTo(expectedResult));
            Assert.That(task2.Result, Is.EqualTo(expectedResult));
            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(45), $"Expected parallel execution (>= 50ms), but was {elapsedTime.TotalMilliseconds}ms");
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(100), $"No throttling delay expected, but was {elapsedTime.TotalMilliseconds}ms");
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
        public async Task ExecuteWithThrottle_EmptyApiKey_ThrowsArgumentException()
        {
            // Arrange
            var throttle = new ApiThrottle();
            Func<Task<string>> apiCall = () => Task.FromResult("TestResult");

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await throttle.ExecuteWithThrottle("", apiCall));
        }

        [Test]
        public async Task ExecuteWithThrottle_WhitespaceApiKey_ThrowsArgumentException()
        {
            // Arrange
            var throttle = new ApiThrottle();
            Func<Task<string>> apiCall = () => Task.FromResult("TestResult");

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await throttle.ExecuteWithThrottle(" ", apiCall));
        }

        [Test]
        public async Task ExecuteWithThrottle_NullInterval_UsesDefaultInterval()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
            string apiKey = "test-key";
            var expectedResult = "TestResult";
            Func<Task<string>> apiCall = () => Task.FromResult(expectedResult);

            // Erster Aufruf
            await throttle.ExecuteWithThrottle(apiKey, apiCall, null);

            // Act: Zweiter Aufruf (defaultInterval = 1s)
            var startTime = DateTime.Now;
            var result = await throttle.ExecuteWithThrottle(apiKey, apiCall, null);
            var elapsedTime = DateTime.Now - startTime;

            // Assert: Default-Intervall (1s ±20% Jitter = 800-1200ms)
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(elapsedTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(780), $"Expected delay around 1000ms, but was {elapsedTime.TotalMilliseconds}ms");
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThanOrEqualTo(1220), $"Expected delay around 1000ms, but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public async Task ExecuteWithThrottle_ZeroInterval_NoDelay()
        {
            // Arrange
            var throttle = new ApiThrottle();
            string apiKey = "test-key";
            var expectedResult = "TestResult";
            Func<Task<string>> apiCall = () => Task.FromResult(expectedResult);
            var interval = TimeSpan.Zero;

            // Act
            var startTime = DateTime.Now;
            var result = await throttle.ExecuteWithThrottle(apiKey, apiCall, interval);
            var elapsedTime = DateTime.Now - startTime;

            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(50), $"No delay expected for zero interval, but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public async Task ExecuteWithThrottle_FailingApiCall_DifferentExceptions_ReleasesSemaphore()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 1);
            string apiKey = "test-key";
            Func<Task<string>> failingApiCall1 = () => throw new InvalidOperationException("API failed");
            Func<Task<string>> failingApiCall2 = () => throw new TaskCanceledException("API canceled");

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await throttle.ExecuteWithThrottle(apiKey, failingApiCall1));
            Assert.ThrowsAsync<TaskCanceledException>(async () => await throttle.ExecuteWithThrottle(apiKey, failingApiCall2));

            // Prüfe, ob Semaphore freigegeben wurde
            var successfulCall = await throttle.ExecuteWithThrottle(apiKey, () => Task.FromResult("Success"));
            Assert.That(successfulCall, Is.EqualTo("Success"));
        }

        [Test]
        public async Task ExecuteWithThrottle_HighConcurrency_HandlesMultipleCalls()
        {
            // Arrange
            var throttle = new ApiThrottle(maxConcurrentCalls: 2);
            string apiKey = "test-key";
            var delay = TimeSpan.FromMilliseconds(100);
            Func<Task<string>> apiCall = async () =>
            {
                await Task.Delay(delay);
                return "TestResult";
            };
            var tasks = new List<Task<string>>();
            int callCount = 10;

            // Act: Starte viele Aufrufe
            var startTime = DateTime.Now;
            for (int i = 0; i < callCount; i++)
            {
                tasks.Add(throttle.ExecuteWithThrottle($"key-{i}", apiCall, TimeSpan.FromMilliseconds(50)));
            }
            await Task.WhenAll(tasks);
            var elapsedTime = DateTime.Now - startTime;

            // Assert: Mit maxConcurrentCalls=2 sollten 10 Aufrufe in ca. 5*100ms=500ms laufen
            Assert.That(tasks.All(t => t.Result == "TestResult"));
            Assert.That(elapsedTime.TotalMilliseconds, Is.LessThan(700), $"Expected ~500ms for 10 calls with maxConcurrentCalls=2, but was {elapsedTime.TotalMilliseconds}ms");
        }

        [Test]
        public void ApiThrottle_Constructor_ZeroMaxConcurrentCalls_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new ApiThrottle(maxConcurrentCalls: 0));
        }
    }
}