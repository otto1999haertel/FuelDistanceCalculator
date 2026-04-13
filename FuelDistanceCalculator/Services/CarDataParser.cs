using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FuelDistanceCalculator.Services;

public static class CarDataParser
{
    private async static Task<JObject> DeserializeFile(string jsonFilePath)
    {
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        return JsonConvert.DeserializeObject<JObject>(jsonContent)
            ?? throw new Exception("Failed to deserialize JSON");
    }
    public static async  Task<Dictionary<string, decimal>> ParseCarData(string jsonFilePath)
    {
        if(!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"The specified JSON file was not found: {jsonFilePath}");
        }
        Dictionary<string, decimal> carsAndRespectivePricePerkm = new Dictionary<string, decimal>();
        var jsonObject = await DeserializeFile(jsonFilePath);
        carsAndRespectivePricePerkm = jsonObject["cars"].ToObject<Dictionary<string, decimal>>();;
        return carsAndRespectivePricePerkm;
    }

    public static async Task<Dictionary<string, string>> GetMetaData(string jsonContent)
    {
        Dictionary<string, string> metaData = new Dictionary<string, string>();
        var jsonObject = await DeserializeFile(jsonContent);
        metaData  = jsonObject["metadata"].ToObject<Dictionary<string, string>>();
        return metaData;
    }
}