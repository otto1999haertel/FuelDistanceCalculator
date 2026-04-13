using FuelDistanceCalculator.Services;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class DiscountParserTest : ServiceTestBase
{
    [Test]
    [TestCase("10%", 0.1,true)]
    [TestCase("0%", 0.0, true)]
    [TestCase("100%", 1, true)]
    [TestCase("15.5%", 0.155, true)]
    [TestCase("10",10, false)]
    [TestCase("5",5, false)]
    [TestCase("-5%",0, false)]
    [TestCase("101%",0, false)]
    public void ParseDiscountPercentage_ValidInput_ReturnsExpected(string input, decimal expected, bool parseable)
    {
        decimal discountValue;
        // Act
        Assert.That(DiscountParser.TryParseDiscountPercent(input, out discountValue).Equals(parseable));
        if (parseable){
            Assert.That(discountValue, Is.EqualTo(expected));
        }
    }
}