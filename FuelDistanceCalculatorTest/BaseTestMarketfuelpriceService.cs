namespace FuelDistanceCalculatorTest;

public  class BaseTestMarketfuelpriceService
{
    //Basisklasse für alle Tests der APP => holt sich eine fake Tankstellenliste die im Setup erstellt und validiert wird
    private List<GasStation> _fakeGasStationList;
    [SetUp]
    public void Setup()
    {
        //initiate 
        //_fakeGasStationList = 
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}
