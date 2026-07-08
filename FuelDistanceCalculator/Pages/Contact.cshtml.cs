using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FuelDistanceCalculator.Pages;

public class ContactModel : PageModel
{
    private readonly ILogger<ContactModel> _logger;

    public string Name => ContactInfo.Name;

    [BindProperty]
    public bool IsProduction { get; private set; }

    public ContactModel(ILogger<ContactModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        IsProduction = configuration["MODE_TYPE"]?.Equals("Production") == true;
    }



    public void OnGet()
    {
        // Setzen des Namens in ViewData, damit er im Layout verfügbar ist
        ViewData["ContactName"] = ContactInfo.Name;
    }
}
