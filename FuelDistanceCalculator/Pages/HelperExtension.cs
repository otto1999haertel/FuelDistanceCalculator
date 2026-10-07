// FuelDistanceCalculator/Extensions/HtmlHelperExtensions.cs
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FuelDistanceCalculator.Extensions;

public static class HtmlHelperExtensions
{
    public static string? CspNonce(this IHtmlHelper html) =>
        html.ViewContext.HttpContext.Items["csp-nonce"] as string;
}