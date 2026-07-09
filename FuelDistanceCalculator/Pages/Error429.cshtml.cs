using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class Error429Model : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public void OnGet()
    {
        // Wichtig: korrekter Statuscode für Monitoring / SEO
        Response.StatusCode = StatusCodes.Status429TooManyRequests;
        // Request-ID für Logging / Support
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }

}