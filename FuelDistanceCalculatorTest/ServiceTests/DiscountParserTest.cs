using FuelDistanceCalculator.Services;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class DiscountParserTest : ServiceTestBase
{

    [Test]
    [TestCase("10", 10, true)]
    [TestCase("-10", 0, false)]
    [TestCase("0", 0, true)]
    [TestCase("15.5", 15.5, true)]
    [TestCase("abc", 0, false)]
    public void ParseDiscountTotalAbsolute_ValidInput_ReturnsExpected(string input, decimal expected, bool parseable)
    {
        decimal discountValue;
        // Act
        Assert.That(DiscountParser.TryParseDiscountOnTotalAbsolut(input, out discountValue).Equals(parseable));
        if (parseable)
        {
            Assert.That(discountValue, Is.EqualTo(expected));
        }
    }
    [Test]
    [TestCase("10%", 0.1, true)]
    [TestCase("0%", 0.0, true)]
    [TestCase("100%", 1, true)]
    [TestCase("15.5%", 0.155, true)]
    [TestCase("10", 10, false)]
    [TestCase("5", 5, false)]
    [TestCase("-5%", 0, false)]
    [TestCase("101%", 0, false)]
    [TestCase(("abc%"), 0, false)]
    [TestCase("abc0,1", 0, false)]
    public void ParseDiscountPercentage_ValidInput_ReturnsExpected(string input, decimal expected, bool parseable)
    {
        decimal discountValue;
        // Act
        Assert.That(DiscountParser.TryParseDiscountOnUnitPercentage(input, out discountValue).Equals(parseable));
        if (parseable)
        {
            Assert.That(discountValue, Is.EqualTo(expected));
        }
    }

    [Test]
    [TestCase("10ct", 0.10, true)]
    [TestCase("0ct", 0, true)]
    [TestCase("15.5ct", 0.155, true)]
    [TestCase("10", 0, false)]
    [TestCase("5", 0, false)]
    [TestCase("-5ct", 0, false)]
    [TestCase("100ct", 1, true)]
    [TestCase("101ct", 1.01, true)]
    [TestCase("abcct", 0, false)]
    public void ParseDiscountOnUnit_ValidInput_ReturnsExpected(string input, decimal expected, bool parseable)
    {
        decimal discountValue;
        // Act
        Assert.That(DiscountParser.TryParseDiscountOnUnitAbsolut(input, out discountValue).Equals(parseable));
        if (parseable)
        {
            Assert.That(discountValue, Is.EqualTo(expected));       
        }
    }
}