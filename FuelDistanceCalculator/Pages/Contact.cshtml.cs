using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FuelDistanceCalculator.Pages;

public class ContactModel : PageModel
{
    private readonly ILogger<ContactModel> _logger;

    public string Name => ContactInfo.Name;
    public string Email => ContactInfo.Email;

    [BindProperty]
    public bool  IsProduction { get; private set; }

    public ContactModel(ILogger<ContactModel> logger)
    {
        _logger = logger;
        IsProduction = Environment.GetEnvironmentVariable("MODE_TYPE")?.Equals("Production") == true;
    }



    public void OnGet()
    {
            // Setzen des Namens in ViewData, damit er im Layout verfügbar ist
            ViewData["ContactName"] = ContactInfo.Name;
    }
}