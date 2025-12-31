// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WebDebug.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// PageModel for the Privacy page.
/// Renders a static privacy policy view.
/// </summary>
public class PrivacyModel : PageModel
{
    /// <summary>
    /// Handles GET requests for the page.
    /// No server-side processing is required; the page is rendered as-is.
    /// </summary>
    public void OnGet()
    {
    }
}
