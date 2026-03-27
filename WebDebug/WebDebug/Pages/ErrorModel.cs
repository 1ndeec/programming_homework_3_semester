// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WebDebug.Pages;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Configures the error page to never be cached, so users don't see stale error responses.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
/// <summary>
/// Disables antiforgery validation for the error page, since it may be reached in unusual failure scenarios.
/// </summary>
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    /// <summary>
    /// Gets or sets identifier for the current request, useful for correlating logs and diagnostics.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets a value indicating whether a request id is available and should be shown on the error page.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

    /// <summary>
    /// Handles GET requests for the error page.
    /// Captures the current Activity id if present, otherwise falls back to the HTTP trace identifier.
    /// </summary>
    public void OnGet()
    {
        this.RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
    }
}
